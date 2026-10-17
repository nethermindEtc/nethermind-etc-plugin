// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;

namespace Nethermind.EthereumClassic.Config;

public class EtcMiningConfig : IEtcMiningConfig
{
    public EtcMiningMode Mode { get; set; } = EtcMiningMode.None;

    public int WorkRefreshSeconds { get; set; } = 4;

    public Address? Coinbase { get; set; }
}
