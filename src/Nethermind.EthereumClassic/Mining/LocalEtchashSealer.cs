// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Threading;
using System.Threading.Tasks;
using Nethermind.Consensus;
using Nethermind.Consensus.Ethash;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Crypto;
using Nethermind.Logging;

namespace Nethermind.EthereumClassic.Mining;

/// <summary>
/// Sealer that performs local CPU mining using Etchash.
/// </summary>
internal sealed class LocalEtchashSealer : ISealer
{
    private readonly IEthash _ethash;
    private readonly Address _coinbase;
    private readonly ILogger _logger;

    public LocalEtchashSealer(IEthash ethash, Address coinbase, ILogManager logManager)
    {
        _ethash = ethash ?? throw new ArgumentNullException(nameof(ethash));
        _coinbase = coinbase ?? throw new ArgumentNullException(nameof(coinbase));
        _logger = logManager?.GetClassLogger<LocalEtchashSealer>() ?? throw new ArgumentNullException(nameof(logManager));
    }

    public Address Address => _coinbase;

    public bool CanSeal(ulong blockNumber, Hash256 parentHash) => true;

    public async Task<Block> SealBlock(Block block, CancellationToken cancellationToken)
    {
        ValidateBlock(block);

        Block sealedBlock = await Task.Factory.StartNew(() => Mine(block), cancellationToken);
        sealedBlock.Header.Hash = sealedBlock.Header.CalculateHash();
        return sealedBlock;
    }

    private Block Mine(Block block)
    {
        if (_logger.IsInfo) _logger.Info($"Starting CPU mining for block {block.Number}");

        (Hash256 mixHash, ulong nonce) = _ethash.Mine(block.Header, startNonce: null);

        block.Header.Nonce = nonce;
        block.Header.MixHash = mixHash;

        if (_logger.IsInfo) _logger.Info($"Mined block {block.Number} with nonce {nonce:X16}");

        return block;
    }

    private static void ValidateBlock(Block block)
    {
        if (block.Header.TxRoot is null ||
            block.Header.StateRoot is null ||
            block.Header.ReceiptsRoot is null ||
            block.Header.UnclesHash is null ||
            block.Header.Bloom is null ||
            block.Header.ExtraData is null)
        {
            throw new InvalidOperationException($"Requested to mine an invalid block {block.Header}");
        }
    }
}
