// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Micasa.Cli.Models
{
    public class CommandRunnerResult
    {
        public TimeSpan? Duration { get; set; }
        public string? StandardOutput { get; set; }
        public string? StandardError { get; set; }
        public int? ExitCode { get; set; }
    }
}
