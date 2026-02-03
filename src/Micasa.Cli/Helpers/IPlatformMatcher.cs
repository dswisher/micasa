// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace Micasa.Cli.Helpers
{
    public interface IPlatformMatcher
    {
        string? FindBestMatch(string platformName, ICollection<string> availablePlatforms);
    }
}
