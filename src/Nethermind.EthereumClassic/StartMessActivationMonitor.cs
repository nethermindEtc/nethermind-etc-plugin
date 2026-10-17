// SPDX-FileCopyrightText: 2025 Ethereum Classic Community
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;
using Nethermind.Api.Steps;
using Nethermind.Core;
using Nethermind.Init.Steps;
using Nethermind.Logging;
using Nethermind.Synchronization.Peers;

namespace Nethermind.EthereumClassic;

/// <summary>
/// Starts the MESS (ECBP-1100) activation monitor once the network is up.
/// </summary>
/// <remarks>
/// Only registered when <c>EtcMess.Enabled</c> is set, since it needs <see cref="EtcBlockTree"/>.
/// </remarks>
[RunnerStepDependencies(
    dependencies: [typeof(InitializeNetwork)],
    dependents: []
)]
internal class StartMessActivationMonitor(
    EtcBlockTree blockTree,
    ISyncPeerPool syncPeerPool,
    ITimestamper timestamper,
    ILogManager logManager,
    IDisposableStack disposeStack) : IStep
{
    public Task Execute(CancellationToken cancellationToken)
    {
        MessActivationMonitor monitor = new(blockTree, syncPeerPool, timestamper, logManager);
        disposeStack.Push(monitor);
        monitor.Start();

        return Task.CompletedTask;
    }
}
