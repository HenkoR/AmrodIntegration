// ==========================================================================
//  Amrod Woocommerce Integration
// ==========================================================================
//  Copyright (c) AmrodIntegration (Henko Rabie)
//  All rights reserved. Licensed under the GNU General Public License.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AmrodWCIntegration.Clients;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmrodWCIntegration.Config
{
    public static class ServicesExtensions
    {
        public static void AddClientsOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<WcOptions>(configuration.GetSection("WC"));
            services.Configure<WpOptions>(configuration.GetSection("WP"));
        }

        public static void AddClients(this IServiceCollection services)
        {
            services.AddTransient<WoocommerceClient>();
        }
    }
}
