// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Models;

namespace Micasa.Cli.Serialization
{
    public class FormulaReader : IFormulaReader
    {
        private readonly Assembly assembly = typeof(FormulaReader).Assembly;
        private readonly JsonSerializerOptions serializerOptions = new()
        {
            PropertyNamingPolicy = new JsonKebabCaseNamingPolicy(),
            ReadCommentHandling = JsonCommentHandling.Skip
        };


        public async Task<Formula?> ReadFormulaAsync(string formulaName, CancellationToken stoppingToken)
        {
            // Build the path
            var path = $"Micasa.Cli.Formulary.{formulaName}.json";

            // Load the content
            Formula? formula;
            await using (var stream = assembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                {
                    return null;
                }

                formula = await JsonSerializer.DeserializeAsync<Formula>(stream, serializerOptions, stoppingToken);
            }

            // I'm not sure how it could ever be null here, but...
            if (formula == null)
            {
                return null;
            }

            // Find all the composite keys (like "ubuntu|debian") and replace those entries with individual entries.
            var compositeKeys = formula.Platforms.Keys.Where(x => x.Contains('|')).ToList();
            foreach (var key in compositeKeys)
            {
                // Get the directive
                var directive = formula.Platforms[key];

                // Split the key into individual keys
                var individualKeys = key.Split('|');

                // Add each individual key to the dictionary
                foreach (var individualKey in individualKeys)
                {
                    formula.Platforms[individualKey] = directive;
                }

                // Remove the composite key
                formula.Platforms.Remove(key);
            }

            // Return the result of all of that
            return formula;
        }
    }
}
