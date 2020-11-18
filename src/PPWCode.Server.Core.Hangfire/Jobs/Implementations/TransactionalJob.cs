// Copyright 2020 by PeopleWare n.v..
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Castle.Core.Logging;

using JetBrains.Annotations;

using NHibernate;

using PPWCode.Server.Core.Hangfire.Jobs.Interfaces;
using PPWCode.Server.Core.RequestContext.Interfaces;

namespace PPWCode.Server.Core.Hangfire.Jobs.Implementations
{
    public abstract class TransactionalJob : ITransactionalJob
    {
        private ILogger _logger = NullLogger.Instance;

        protected TransactionalJob(
            [NotNull] IRequestContext requestContext,
            [NotNull] ISession session)
        {
            Session = session;
            RequestContext = requestContext;
        }

        [NotNull]
        public IRequestContext RequestContext { get; }

        [NotNull]
        public ISession Session { get; }

        public CancellationToken CancellationToken
            => RequestContext.RequestAborted;

        [NotNull]
        [UsedImplicitly]
        public ILogger Logger
        {
            get => _logger;
            set
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (value != null)
                {
                    _logger = value;
                }
            }
        }

        [NotNull]
        protected virtual Task RunAsync(
            [NotNull] string requestDescription,
            [NotNull] Func<CancellationToken, Task> lambda,
            CancellationToken cancellationToken)

        {
            async Task<int> WrapperAsync(CancellationToken can)
            {
                await lambda(can).ConfigureAwait(false);
                return default;
            }

            return RunAsync(requestDescription, WrapperAsync, cancellationToken);
        }

        [NotNull]
        [ItemCanBeNull]
        protected virtual Task<TResult> RunAsync<TResult>(
            [NotNull] string requestDescription,
            [NotNull] Func<CancellationToken, Task<TResult>> lambda,
            CancellationToken cancellationToken)
        {
            if (lambda == null)
            {
                throw new ArgumentNullException(nameof(lambda));
            }

            string StartMessage()
                => $"Job {requestDescription} started.";

            string FinishMessage()
                => $"Job {requestDescription} finished.";

            return RunAsync(StartMessage, FinishMessage, lambda, cancellationToken);
        }

        [NotNull]
        [ItemCanBeNull]
        protected virtual async Task<TResult> RunAsync<TResult>(
            [NotNull] Func<string> startMessage,
            [NotNull] Func<string> finishedMessage,
            [NotNull] Func<CancellationToken, Task<TResult>> lambda,
            CancellationToken cancellationToken)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Logger.Info(startMessage);

            TResult result;
            try
            {
                ITransaction transaction = Session.BeginTransaction(IsolationLevel.Unspecified);
                try
                {
                    result = await lambda(cancellationToken).ConfigureAwait(false);
                    if (cancellationToken.CanBeCanceled && cancellationToken.IsCancellationRequested)
                    {
                        await RollbackAsync(transaction).ConfigureAwait(false);
                    }
                    else
                    {
                        await CommitAsync(transaction, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"job, {GetType().FullName}, roll-backed because of a cancellation request", e);
                    await RollbackAsync(transaction).ConfigureAwait(false);
                    throw;
                }
                finally
                {
                    transaction?.Dispose();
                }
            }
            finally
            {
                sw.Stop();
                Logger.Info(() => $"{finishedMessage()}, elapsed {sw.ElapsedMilliseconds} ms.");
            }

            return result;
        }

        private async Task RollbackAsync([CanBeNull] ITransaction transaction)
        {
            if (Session.IsOpen)
            {
                if (transaction?.IsActive == true)
                {
                    await transaction.RollbackAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task CommitAsync([CanBeNull] ITransaction transaction, CancellationToken cancellationToken)
        {
            if (Session.IsOpen)
            {
                await Session.FlushAsync(cancellationToken);
                if (transaction?.IsActive == true)
                {
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
