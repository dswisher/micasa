// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Micasa.Cli.Options.Common;

namespace Micasa.Cli.Options
{
    [Verb("info", HelpText = "Display information about a specific formula.")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Instantiated by CommandLineParser")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Set by CommandLineParser")]
    public class InfoOptions : ILogOptions
    {
        // ILogOptions
        [Option("verbose", HelpText = "Enable verbose (debug) logging.")]
        public bool Verbose { get; set; }

        // Command-specific options
        [Value(1, HelpText = "The name of the formula to install.")]
        public required string FormulaName { get; set; }
    }
}
