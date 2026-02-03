// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Micasa.Cli.Commands;
using Micasa.Cli.Options;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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
                    InstallOptions,
                    UninstallOptions>(args);

                // Set up logging
                var logConfig = new LoggerConfiguration()
                    .WriteTo.Console();

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
                        await parsedArgs.WithParsedAsync<InstallOptions>(options => scope.ServiceProvider.GetRequiredService<InstallCommand>().ExecuteAsync(options, tokenSource.Token));
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
