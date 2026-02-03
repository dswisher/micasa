// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Micasa.Cli.Helpers
{
    public class PlatformMatcher : IPlatformMatcher
    {
        public string? FindBestMatch(string platformName, ICollection<string> availablePlatforms)
        {
            // Simple exact match
            if (availablePlatforms.Contains(platformName))
            {
                return platformName;
            }

            // Fallback to more general matches by progressively stripping parts from the right
            var platformParts = platformName.Split('-');
            for (int i = platformParts.Length - 1; i > 0; i--)
            {
                var generalPlatform = string.Join("-", platformParts.Take(i));
                if (availablePlatforms.Contains(generalPlatform))
                {
                    return generalPlatform;
                }
            }

            return null;
        }
    }
}
