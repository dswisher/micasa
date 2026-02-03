// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
        private readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new JsonKebabCaseNamingPolicy()
        };


        public async Task<Formula?> ReadFormulaAsync(string formulaName, CancellationToken stoppingToken)
        {
            // Build the path
            var path = $"Micasa.Cli.Formulary.{formulaName}.json";

            await using (var stream = assembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                {
                    return null;
                }

                return await JsonSerializer.DeserializeAsync<Formula>(stream, serializerOptions, stoppingToken);
            }
        }
    }
}
