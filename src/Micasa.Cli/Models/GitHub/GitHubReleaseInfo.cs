// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace Micasa.Cli.Models.GitHub
{
    public class GitHubReleaseInfo
    {
        public required string Name { get; set; }

        public required List<GitHubAsset> Assets { get; set; }
    }
}
