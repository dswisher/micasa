// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Micasa.Cli.Helpers;
using Micasa.Cli.Models.GitHub;

namespace Micasa.Cli.GitHub
{
    public class GitHubArchiveSeeker : IGitHubArchiveSeeker
    {
        private readonly IPlatformDecoder platformDecoder;

        public GitHubArchiveSeeker(IPlatformDecoder platformDecoder)
        {
            this.platformDecoder = platformDecoder;
        }


        public GitHubAsset FindBestAsset(GitHubReleaseInfo releaseInfo)
        {
            var arch = platformDecoder.SystemArchitecture;

            // Get the preferred architecture name and alternates
            var (preferredArch, alternateArch) = GetArchitectureNames(arch);

            // Filter assets for Linux platform
            var linuxAssets = releaseInfo.Assets
                .Where(a => a.Name.Contains("linux", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (linuxAssets.Count == 0)
            {
                throw new InvalidOperationException("No Linux assets found");
            }

            // Try to find the best match using preferred architecture name
            var bestAsset = FindBestMatch(linuxAssets, preferredArch);
            if (bestAsset != null)
            {
                return bestAsset;
            }

            // Try alternate architecture name
            bestAsset = FindBestMatch(linuxAssets, alternateArch);
            if (bestAsset != null)
            {
                return bestAsset;
            }

            throw new InvalidOperationException($"No suitable Linux asset found for architecture {arch}");
        }


        private static GitHubAsset? FindBestMatch(List<GitHubAsset> assets, string archName)
        {
            // Filter to assets containing the architecture name
            var archAssets = assets
                .Where(a => a.Name.Contains(archName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (archAssets.Count == 0)
            {
                return null;
            }

            // Score and sort assets by preference
            var scoredAssets = archAssets
                .Select(a => new
                {
                    Asset = a,
                    Score = CalculateAssetScore(a.Name)
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            return scoredAssets[0].Asset;
        }


        private static int CalculateAssetScore(string name)
        {
            var score = 0;

            // Prefer tar.gz files (highest priority)
            if (name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                score += 1000;
            }

            // Prefer appimage files (second priority)
            if (name.EndsWith(".appimage", StringComparison.OrdinalIgnoreCase))
            {
                score += 900;
            }

            // Prefer "musl" over "gnu"
            if (name.Contains("musl", StringComparison.OrdinalIgnoreCase))
            {
                score += 90;
            }
            else if (name.Contains("gnu", StringComparison.OrdinalIgnoreCase))
            {
                score += 50;
            }

            // Deprioritize deb files
            if (name.EndsWith(".deb", StringComparison.OrdinalIgnoreCase))
            {
                score -= 500;
            }

            return score;
        }


        private static (string Preferred, string Alternate) GetArchitectureNames(string arch)
        {
            return arch.ToLowerInvariant() switch
            {
                "amd64" => ("x86_64", "amd64"),
                "arm64" => ("aarch64", "arm64"),
                _ => (arch, arch)
            };
        }
    }
}
