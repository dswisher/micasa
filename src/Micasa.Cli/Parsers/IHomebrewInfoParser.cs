// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Micasa.Cli.Models;

namespace Micasa.Cli.Parsers
{
    public interface IHomebrewInfoParser
    {
        FormulaDetails Parse(string stdout);
    }
}
