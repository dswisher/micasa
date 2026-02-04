// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Micasa.Cli.Helpers
{
    public class PlatformDecoder : IPlatformDecoder
    {
        private readonly ICommandRunner commandRunner;

        private static readonly Dictionary<int, string> MacOsVersionMap = new Dictionary<int, string>
        {
            { 10, "catalina" },
            { 11, "big-sur" },
            { 12, "monterey" },
            { 13, "ventura" },
            { 14, "sonoma" },
            { 15, "sequoia" }
        };

        public PlatformDecoder(ICommandRunner commandRunner)
        {
            this.commandRunner = commandRunner;
        }

        public string GetPlatformName()
        {
            var os = GetOperatingSystem();
            var version = GetOsVersion();
            var arch = GetArchitecture();

            return $"{os}-{version}-{arch}";
        }

        private static string GetOperatingSystem()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "macos";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxDistribution();
            }

            throw new PlatformNotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}");
        }

        private string GetOsVersion()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacOsVersion();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxVersion();
            }

            throw new PlatformNotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}");
        }

        private string GetMacOsVersion()
        {
            var result = commandRunner.RunCommandAsync("sw_vers", "-productVersion", CancellationToken.None).GetAwaiter().GetResult();

            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                throw new InvalidOperationException($"Failed to get macOS version: {result.StandardError}");
            }

            var version = result.StandardOutput.Trim();
            var parts = version.Split('.');

            if (parts.Length < 1 || !int.TryParse(parts[0], out var majorVersion))
            {
                throw new InvalidOperationException($"Failed to parse macOS version: {version}");
            }

            if (!MacOsVersionMap.TryGetValue(majorVersion, out var codename))
            {
                throw new InvalidOperationException($"Unknown macOS version: {majorVersion}");
            }

            return codename;
        }


        private static string GetLinuxDistribution()
        {
            var osReleasePath = "/etc/os-release";
            if (!File.Exists(osReleasePath))
            {
                throw new InvalidOperationException("Unable to determine Linux distribution: /etc/os-release not found");
            }

            var lines = File.ReadAllLines(osReleasePath);
            foreach (var line in lines)
            {
                if (line.StartsWith("ID=", StringComparison.Ordinal))
                {
                    var distro = line.Substring("ID=".Length).Trim('"').ToLowerInvariant();

                    // Map distribution names to our platform names
                    return distro switch
                    {
                        "ubuntu" => "ubuntu",
                        "debian" => "debian",
                        "amzn" => "amazonlinux",
                        _ => throw new PlatformNotSupportedException($"Unsupported Linux distribution: {distro}")
                    };
                }
            }

            throw new InvalidOperationException("Unable to determine Linux distribution from /etc/os-release");
        }


        private string GetLinuxVersion()
        {
            // Try reading /etc/os-release first
            var osReleasePath = "/etc/os-release";
            if (File.Exists(osReleasePath))
            {
                var lines = File.ReadAllLines(osReleasePath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("VERSION_CODENAME=", StringComparison.Ordinal))
                    {
                        return line.Substring("VERSION_CODENAME=".Length).Trim('"');
                    }

                    if (line.StartsWith("UBUNTU_CODENAME=", StringComparison.Ordinal))
                    {
                        return line.Substring("UBUNTU_CODENAME=".Length).Trim('"');
                    }
                }
            }

            // Fallback to lsb_release command
            var result = commandRunner.RunCommandAsync("lsb_release", "-cs", CancellationToken.None).GetAwaiter().GetResult();

            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                throw new InvalidOperationException($"Failed to get Linux version: {result.StandardError}");
            }

            return result.StandardOutput.Trim();
        }

        private static string GetArchitecture()
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "amd64",
                Architecture.Arm64 => "arm64",
                Architecture.X86 => throw new PlatformNotSupportedException("32-bit x86 architecture is not supported"),
                Architecture.Arm => throw new PlatformNotSupportedException("32-bit ARM architecture is not supported"),
                _ => throw new PlatformNotSupportedException($"Unsupported architecture: {RuntimeInformation.OSArchitecture}")
            };
        }
    }
}
