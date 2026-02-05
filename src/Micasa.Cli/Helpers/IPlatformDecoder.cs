// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Micasa.Cli.Helpers
{
    public interface IPlatformDecoder
    {
        string OperatingSystem { get; }
        string OsVersion { get; }
        string SystemArchitecture { get; }

        string PlatformName { get; }
    }
}
