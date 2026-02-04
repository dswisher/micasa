// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Micasa.Cli.Models;

namespace Micasa.Cli.Drivers
{
    public interface IDriverFactory
    {
        Task<DriverFactoryResult> GetDriverForFormulaAsync(string formulaName, CancellationToken stoppingToken);
    }
}
