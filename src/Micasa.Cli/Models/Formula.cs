// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace Micasa.Cli.Models
{
    public class Formula
    {
        public required string FormulaId { get; set; }
        public required Dictionary<string, InstallerDirective> Platforms { get; set; }
    }
}
