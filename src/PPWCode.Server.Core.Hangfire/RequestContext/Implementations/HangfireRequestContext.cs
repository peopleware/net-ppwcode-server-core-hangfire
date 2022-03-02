// Copyright 2020-2022 by PeopleWare n.v..
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
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;

using Hangfire.Server;

using JetBrains.Annotations;

using PPWCode.Server.Core.RequestContext.Implementations;
using PPWCode.Server.Core.RequestContext.Interfaces;
using PPWCode.Vernacular.Exceptions.IV;
using PPWCode.Vernacular.Persistence.IV;

namespace PPWCode.Server.Core.Hangfire.RequestContext.Implementations
{
    /// <inheritdoc cref="IRequestContext" />
    [UsedImplicitly]
    public class HangfireRequestContext : AbstractRequestContext
    {
        private IPrincipal _principal;
        private DateTime? _requestTimestamp;
        private string _traceIdentifier;

        /// <inheritdoc />
        public HangfireRequestContext(
            [NotNull] ITimeProvider timeProvider,
            [NotNull] PerformContext performContext)
            : base(timeProvider)
        {
            PerformContext = performContext;
        }

        [NotNull]
        public PerformContext PerformContext { get; }

        /// <inheritdoc />
        public override DateTime RequestTimestamp
            => _requestTimestamp ??= TimeProvider.UtcNow;

        /// <inheritdoc />
        public override CancellationToken RequestAborted
            => PerformContext.CancellationToken?.ShutdownToken ?? CancellationToken.None;

        /// <inheritdoc />
        public override bool IsReadOnly
            => false;

        /// <inheritdoc />
        public override IPrincipal User
            => (_principal ??= Thread.CurrentPrincipal) ?? throw new ProgrammingError("Euh, no principal found on current thread");

        /// <inheritdoc />
        public override string TraceIdentifier
            => _traceIdentifier ??= Guid.NewGuid().ToString("D");

        /// <inheritdoc />
        public override string Link(string route, IDictionary<string, object> parameters)
            => null;
    }
}
