// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Models;
using Microsoft.Extensions.Logging;

namespace Micasa.Cli.Helpers
{
    public class CommandRunner : ICommandRunner
    {
        private readonly ILogger<CommandRunner> logger;

        public CommandRunner(ILogger<CommandRunner> logger)
        {
            this.logger = logger;
        }


        public async Task<CommandRunnerResult> RunCommandAsync(string command, string arguments, CancellationToken stoppingToken)
        {
            var workDir = Directory.GetCurrentDirectory();

            return await RunCommandAsync(command, arguments, workDir, stoppingToken);
        }


        public async Task<CommandRunnerResult> RunCommandAsync(string command, string arguments, string workDir, CancellationToken stoppingToken)
        {
            var psi = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = workDir,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            // Create the initial scan result that will be populated as we go
            var commandResult = new CommandRunnerResult
            {
                Command = command,
                Arguments = arguments
            };

            // Create a timeout token, so that we don't let commands run forever
            using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken))
            {
                // TODO - make the timeout configurable
                timeoutCts.CancelAfter(TimeSpan.FromMinutes(1));

                // Run the process
                logger.LogDebug("...running {Command} {Arguments}...", psi.FileName, psi.Arguments);

                var commandTimer = Stopwatch.StartNew();

                Process? process = null;
                try
                {
                    using (process = Process.Start(psi))
                    {
                        if (process == null)
                        {
                            throw new Exception("Process is null, trying to run command!");
                        }

                        // Read output streams concurrently to avoid deadlocks
                        var outputTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
                        var errorTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

                        // Wait for process to complete
                        await process.WaitForExitAsync(timeoutCts.Token);

                        // Get the output results
                        commandResult.StandardOutput = await NullIfEmpty(outputTask);
                        commandResult.StandardError = await NullIfEmpty(errorTask);
                        commandResult.ExitCode = process.ExitCode;
                    }
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
                {
                    // Timeout occurred - need to kill the process
                    logger.LogWarning("Command timed out after {Elapsed}, killing process...", commandTimer.Elapsed);

                    try
                    {
                        if (process != null)
                        {
                            process.Kill(entireProcessTree: true);
                            logger.LogInformation("Killed command process due to timeout");
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Process already exited - this is fine, no need to log as a warning
                        logger.LogDebug("Process already exited before kill attempt");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Unexpected error while attempting to kill timed-out process");
                    }
                }
                finally
                {
                    commandResult.Duration = commandTimer.Elapsed;
                }
            }

            // Report and return the result
            logger.LogDebug("...process complete with exit code {ExitCode}, in {Elapsed}.",
                commandResult.ExitCode, commandResult.Duration);

            // Return the result, after all that
            return commandResult;
        }


        public bool VerifyExitCodeZero(CommandRunnerResult statusResult)
        {
            if (statusResult.ExitCode == 0)
            {
                return true;
            }

            logger.LogError("'{Common} {Arguments}' command returned non-zero exit code, {ExitCode}.",
                statusResult.Command, statusResult.Arguments, statusResult.ExitCode);

            if (!string.IsNullOrEmpty(statusResult.StandardError))
            {
                logger.LogInformation("-> Standard Error:\n{StandardError}", statusResult.StandardError);
            }

            return false;
        }


        private static async Task<string?> NullIfEmpty(Task<string> task)
        {
            var content = await task;

            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
    }
}
