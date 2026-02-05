// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Micasa.Cli.Models;

namespace Micasa.Cli.Parsers
{
    public class AptInfoParser : IAptInfoParser
    {
        public FormulaDetails Parse(string stdout)
        {
            var lines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            string? packageId = null;
            string? installedVersion = null;
            string? stableVersion = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Extract package name from the first line ending with ":"
                if (packageId == null && trimmedLine.EndsWith(':'))
                {
                    packageId = trimmedLine.TrimEnd(':');
                    continue;
                }

                // Extract installed version
                if (trimmedLine.StartsWith("Installed:"))
                {
                    var value = trimmedLine.Substring("Installed:".Length).Trim();
                    installedVersion = value == "(none)" ? null : value;
                    continue;
                }

                // Extract stable/candidate version
                if (trimmedLine.StartsWith("Candidate:"))
                {
                    stableVersion = trimmedLine.Substring("Candidate:".Length).Trim();
                }
            }

            if (packageId == null)
            {
                throw new InvalidOperationException("Could not parse package ID from apt-cache output");
            }

            return new FormulaDetails
            {
                PackageId = packageId,
                StableVersion = stableVersion,
                InstalledVersion = installedVersion
            };
        }
    }
}
