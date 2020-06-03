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

using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;

using Hangfire.Server;

using JetBrains.Annotations;

using PPWCode.Server.Core.Hangfire.RequestContext.Implementations;
using PPWCode.Server.Core.RequestContext.Interfaces;

namespace PPWCode.Server.Core.Hangfire.RequestContext.Resolvers
{
    public class HangfireRequestContextResolver : ISubDependencyResolver
    {
        public HangfireRequestContextResolver([NotNull] IKernel kernel)
        {
            Kernel = kernel;
        }

        [NotNull]
        public IKernel Kernel { get; }

        /// <inheritdoc />
        public bool CanResolve(
            [NotNull] CreationContext context,
            [NotNull] ISubDependencyResolver contextHandlerResolver,
            [NotNull] ComponentModel model,
            [NotNull] DependencyModel dependency)
        {
            const string PerformContext = "performContext";

            return (dependency.TargetType == typeof(IRequestContext))
                   && context.AdditionalArguments.Contains(PerformContext)
                   && context.AdditionalArguments[PerformContext] is PerformContext;
        }

        /// <inheritdoc />
        public object Resolve(
            [NotNull] CreationContext context,
            [NotNull] ISubDependencyResolver contextHandlerResolver,
            [NotNull] ComponentModel model,
            [NotNull] DependencyModel dependency)
            => Kernel.Resolve<HangfireRequestContext>(context.AdditionalArguments);
    }
}
