// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Micasa.Cli.Commands;
using Micasa.Cli.Options;
using Micasa.Cli.Options.Common;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Micasa.Cli
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                // Parse the args
                var parsedArgs = Parser.Default.ParseArguments<
                    InfoOptions,
                    InstallOptions,
                    UninstallOptions>(args);

                // Set up logging
                var logConfig = new LoggerConfiguration()
                    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                    .WriteTo.Console();

                if (parsedArgs.Value is ILogOptions lo)
                {
                    if (lo.Verbose)
                    {
                        logConfig.MinimumLevel.Debug();
                    }
                }

                Log.Logger = logConfig.CreateLogger();

                // Do the work
                using (var tokenSource = new CancellationTokenSource())
                await using (var provider = Container.CreateContainer())
                {
                    // shut down semi-gracefully on ctrl+c...
                    Console.CancelKeyPress += (_, eventArgs) =>
                    {
                        Log.Warning("*** Cancel event triggered ***");

                        // ReSharper disable once AccessToDisposedClosure
                        tokenSource.Cancel();
                        eventArgs.Cancel = true;
                    };

                    var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
                    using (var scope = scopeFactory.CreateScope())
                    {
                        await parsedArgs.WithParsedAsync<InfoOptions>(options => scope.ServiceProvider.GetRequiredService<InfoCommand>().ExecuteAsync(options, tokenSource.Token));
                        await parsedArgs.WithParsedAsync<InstallOptions>(options => scope.ServiceProvider.GetRequiredService<InstallCommand>().ExecuteAsync(options, tokenSource.Token));
                        await parsedArgs.WithParsedAsync<UninstallOptions>(options => scope.ServiceProvider.GetRequiredService<UninstallCommand>().ExecuteAsync(options, tokenSource.Token));
                    }
                }

                // No errors!
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return 1;
            }
        }
    }
}
