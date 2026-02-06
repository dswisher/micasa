// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Micasa.Cli.Models.GitHub;

namespace Micasa.Cli.GitHub
{
    public interface IGitHubArchiveSeeker
    {
        GitHubAsset FindBestAsset(GitHubReleaseInfo releaseInfo);
    }
}
