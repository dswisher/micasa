// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Micasa.Cli.Models
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Instantiated by JSON deserializer")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Set by JSON deserializer")]
    public class InstallerDirective
    {
        public required string Tool { get; set; }
        public string? PackageId { get; set; }
    }
}
