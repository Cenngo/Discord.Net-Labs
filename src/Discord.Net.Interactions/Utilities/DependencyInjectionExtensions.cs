using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddInteractionService(this IServiceCollection services, Action<InteractionServiceConfig> configure)
        {
            var config = new InteractionServiceConfig();
            configure(config);
            services.AddSingleton(config);
            services.AddSingleton<InteractionService>();
            return services;
        }
    }
}
