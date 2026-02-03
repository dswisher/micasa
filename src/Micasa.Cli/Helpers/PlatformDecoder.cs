// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Micasa.Cli.Helpers
{
    public class PlatformDecoder : IPlatformDecoder
    {
        public string GetPlatformName()
        {
            // TODO - Implement platform detection logic
            return "macos-sonoma-arm64";
        }
    }
}
