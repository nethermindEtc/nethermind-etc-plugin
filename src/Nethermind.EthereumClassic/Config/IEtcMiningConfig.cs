// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Config;
using Nethermind.Core;

namespace Nethermind.EthereumClassic.Config;

public interface IEtcMiningConfig : IConfig
{
    [ConfigItem(
        Description = "Mining mode: None (disabled), Remote (continuous work for eth_getWork/eth_submitWork), Local (continuous CPU mining), Manual (CPU sealing on evm_mine only, for dev/test chains).",
        DefaultValue = "None")]
    EtcMiningMode Mode { get; set; }

    [ConfigItem(
        Description = "Seconds between block template refreshes in Remote and Local modes, so transactions arriving between blocks get picked up.",
        DefaultValue = "4")]
    int WorkRefreshSeconds { get; set; }

    [ConfigItem(
        Description = "Address that receives block rewards. Required when Mode is not None. Only the address is needed; the node never holds its private key.",
        DefaultValue = "null")]
    Address? Coinbase { get; set; }
}
