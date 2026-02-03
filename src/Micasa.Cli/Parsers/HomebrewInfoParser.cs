// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Micasa.Cli.Models;

namespace Micasa.Cli.Parsers
{
    public class HomebrewInfoParser : IHomebrewInfoParser
    {
        public FormulaDetails Parse(string stdout)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var brewOutput = JsonSerializer.Deserialize<BrewOutput>(stdout, options);

            if (brewOutput?.Formulae == null || brewOutput.Formulae.Count != 1)
            {
                throw new Exception($"Homebrew output contains unexpected number of formulae: {brewOutput?.Formulae.Count ?? 0}");
            }

            var formula = brewOutput.Formulae[0];
            var installedVersion = formula.Installed.Count > 0 ? formula.Installed[0].Version : null;

            var details = new FormulaDetails
            {
                PackageId = formula.Name,
                StableVersion = formula.Versions?.Stable,
                InstalledVersion = installedVersion
            };

            return details;
        }


        private class BrewOutput
        {
            public required List<BrewFormula> Formulae { get; set; }
        }


        private class BrewFormula
        {
            public required string Name { get; set; }
            public required BrewVersion? Versions { get; set; }
            public required List<BrewInstalled> Installed { get; set; }
        }


        private class BrewVersion
        {
            public string? Stable { get; set; }
        }


        private class BrewInstalled
        {
            public string? Version { get; set; }
        }
    }
}
