// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Micasa.Cli.Commands;
using Micasa.Cli.Drivers;
using Micasa.Cli.Helpers;
using Micasa.Cli.Parsers;
using Micasa.Cli.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Micasa.Cli
{
    public static class Container
    {
        public static ServiceProvider CreateContainer()
        {
            // Create the service collection
            var services = new ServiceCollection();

            // Register MSFT logging
            services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));

            // We download stuff using HTTPS
            services.AddHttpClient();

            // General Tooling
            services.AddSingleton<ICommandRunner, CommandRunner>();
            services.AddSingleton<IFormulaReader, FormulaReader>();
            services.AddSingleton<IPlatformDecoder, PlatformDecoder>();
            services.AddSingleton<IPlatformMatcher, PlatformMatcher>();

            // Register drivers with keys matching the Tool string and a factory to resolve them
            services.AddKeyedTransient<IDriver, AdvancedPackageToolDriver>("apt");
            services.AddKeyedTransient<IDriver, ShellScriptDriver>("shell-script");
            services.AddKeyedTransient<IDriver, HomebrewDriver>("homebrew");

            services.AddSingleton<IDriverFactory, DriverFactory>();

            // Parsers for command output
            services.AddSingleton<IAdvancedPackageToolInfoParser, AdvancedPackageToolInfoParser>();
            services.AddSingleton<IHomebrewInfoParser, HomebrewInfoParser>();

            // Register all the commands
            // TODO - should these be transient, scoped, or singleton?
            services.AddScoped<InfoCommand>();
            services.AddScoped<InstallCommand>();
            services.AddScoped<UninstallCommand>();

            // Build and return the container
            return services.BuildServiceProvider(validateScopes: true);
        }
    }
}
