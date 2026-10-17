// SPDX-FileCopyrightText: 2025 Ethereum Classic Community
// SPDX-License-Identifier: Apache-2.0
//
// EtcBlockTree applies MESS (ECBP-1100) antigravity scoring twice. When a block is
// suggested, a rejected fork is stored but never becomes the best suggested block,
// so it cannot shadow the honest chain's extensions. Before a block is processed
// it is scored again against the head as it stands then, since the head may have
// advanced while the block waited in the queue.

using Autofac.Features.AttributeFilters;
using Nethermind.Blockchain;
using Nethermind.Blockchain.BlockAccessLists;
using Nethermind.Blockchain.Blocks;
using Nethermind.Blockchain.Headers;
using Nethermind.Blockchain.Synchronization;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Specs;
using Nethermind.Db;
using Nethermind.Int256;
using Nethermind.Logging;
using Nethermind.State;
using Nethermind.State.Repositories;

namespace Nethermind.EthereumClassic;

internal class EtcBlockTree : BlockTree
{
    private volatile bool _messEnabled;

    public EtcBlockTree(
        IBlockStore? blockStore,
        IHeaderStore? headerDb,
        [KeyFilter("blockInfos")] IDb? blockInfoDb,
        [KeyFilter("metadata")] IDb? metadataDb,
        IBadBlockStore? badBlockStore,
        IBlockAccessListStore? balStore,
        IChainLevelInfoRepository? chainLevelInfoRepository,
        ISpecProvider? specProvider,
        ISyncConfig? syncConfig,
        IStateBoundary? stateBoundary,
        ILogManager? logManager,
        ulong genesisBlockNumber = 0)
        : base(blockStore, headerDb, blockInfoDb, metadataDb, badBlockStore,
            balStore, chainLevelInfoRepository, specProvider, syncConfig,
            stateBoundary, logManager, genesisBlockNumber)
    {
    }

    public void EnableMess(bool enable) => _messEnabled = enable;

    public bool IsMessEnabled => _messEnabled;

    protected override bool BestSuggestedImprovementRequirementsSatisfied(BlockHeader header) =>
        base.BestSuggestedImprovementRequirementsSatisfied(header) && !IsRejectedByMess(header);

    public override bool IsBetterThanHead(BlockHeader? header) =>
        base.IsBetterThanHead(header) && !IsRejectedByMess(header!);

    private bool IsRejectedByMess(BlockHeader header)
    {
        if (!_messEnabled)
            return false;

        BlockHeader? currentHead = Head?.Header;
        if (currentHead is null)
            return false;

        BlockHeader? ancestor = FindCommonAncestor(currentHead, header);
        if (ancestor is null)
            return false;

        // Chain extension (proposed builds directly on head) — never penalize.
        if (ancestor.Hash == currentHead.Hash)
            return false;

        // The ancestor was found without its total difficulty; load it.
        ancestor = FindHeader(ancestor.Hash, BlockTreeLookupOptions.None, ancestor.Number);
        if (ancestor?.TotalDifficulty is null)
            return false;

        UInt256 commonAncestorTD = ancestor.TotalDifficulty.Value;
        UInt256 localTD = currentHead.TotalDifficulty ?? UInt256.Zero;
        UInt256 proposedTD = header.TotalDifficulty ?? UInt256.Zero;

        if (!MessCalculator.ShouldRejectReorg(
                commonAncestorTD,
                localTD,
                proposedTD,
                ancestor.Timestamp,
                currentHead.Timestamp))
        {
            return false;
        }

        if (Logger.IsInfo) Logger.Info(
            $"MESS rejected reorg: ancestor #{ancestor.Number} ({ancestor.Hash}), " +
            $"head #{currentHead.Number}, proposed #{header.Number} ({header.Hash}), " +
            $"timeDelta={currentHead.Timestamp - ancestor.Timestamp}s");

        return true;
    }

    private BlockHeader? FindCommonAncestor(BlockHeader a, BlockHeader b)
    {
        const int maxDepth = 8192;
        int steps = 0;

        // Walk both chains back to the same height, then walk together until hashes match.
        BlockHeader? ha = a;
        BlockHeader? hb = b;

        while (ha is not null && hb is not null && ha.Number > hb.Number && steps < maxDepth)
        {
            ha = FindHeader(ha.ParentHash, BlockTreeLookupOptions.TotalDifficultyNotNeeded);
            steps++;
        }

        while (hb is not null && ha is not null && hb.Number > ha.Number && steps < maxDepth)
        {
            hb = FindHeader(hb.ParentHash, BlockTreeLookupOptions.TotalDifficultyNotNeeded);
            steps++;
        }

        while (ha is not null && hb is not null && ha.Hash != hb.Hash && steps < maxDepth)
        {
            ha = FindHeader(ha.ParentHash, BlockTreeLookupOptions.TotalDifficultyNotNeeded);
            hb = FindHeader(hb.ParentHash, BlockTreeLookupOptions.TotalDifficultyNotNeeded);
            steps++;
        }

        if (ha is not null && hb is not null && ha.Hash == hb.Hash)
            return ha;

        return null;
    }
}
