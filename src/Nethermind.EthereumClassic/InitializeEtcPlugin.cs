// SPDX-FileCopyrightText: 2025 Ethereum Classic Community
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using Nethermind.Api.Steps;
using Nethermind.Config;
using Nethermind.EthereumClassic.Config;
using Nethermind.Init.Steps;
using Nethermind.KeyStore.Config;

namespace Nethermind.EthereumClassic;

/// <summary>
/// Applies the plugin's startup settings and validates its configuration.
/// </summary>
/// <remarks>
/// Runs before <see cref="LoadGenesisBlock"/> so a bad configuration fails the node before the chain is loaded.
/// </remarks>
[RunnerStepDependencies(
    dependencies: [],
    dependents: [typeof(LoadGenesisBlock)]
)]
public class InitializeEtcPlugin(
    IEtcMiningConfig miningConfig,
    IKeyStoreConfig keyStoreConfig) : IStep
{
    public Task Execute(CancellationToken cancellationToken)
    {
        // Set gas token ticker to ETC for logging
        BlocksConfig.GasTokenTicker = "ETC";

        // Validate BlockAuthorAccount when mining is enabled
        if (miningConfig.Mode != EtcMiningMode.None && string.IsNullOrWhiteSpace(keyStoreConfig.BlockAuthorAccount))
        {
            throw new InvalidOperationException(
                $"KeyStore.BlockAuthorAccount is required when EtcMining.Mode is {miningConfig.Mode}");
        }

        return Task.CompletedTask;
    }
}
