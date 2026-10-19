// SPDX-FileCopyrightText: 2025 Ethereum Classic Community
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using Nethermind.Api.Steps;
using Nethermind.Config;
using Nethermind.Core;
using Nethermind.Core.Exceptions;
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

        if (miningConfig.Mode != EtcMiningMode.None)
        {
            if (miningConfig.Coinbase is null || miningConfig.Coinbase == Address.Zero)
            {
                throw new InvalidConfigurationException(
                    $"EtcMining.Coinbase is required when EtcMining.Mode is {miningConfig.Mode}",
                    ExitCodes.ForbiddenOptionValue);
            }

            // Block rewards are paid to EtcMining.Coinbase. KeyStore.BlockAuthorAccount is a signing key,
            // which Etchash never uses, so a leftover value would only fail later on a keystore lookup.
            if (!string.IsNullOrWhiteSpace(keyStoreConfig.BlockAuthorAccount))
            {
                throw new InvalidConfigurationException(
                    "KeyStore.BlockAuthorAccount is not used by Etchash mining; set EtcMining.Coinbase instead",
                    ExitCodes.ForbiddenOptionValue);
            }
        }

        return Task.CompletedTask;
    }
}
