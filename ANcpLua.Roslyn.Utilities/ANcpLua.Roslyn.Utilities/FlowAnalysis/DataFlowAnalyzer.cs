using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ANcpLua.Roslyn.Utilities.FlowAnalysis;

/// <summary>
///     Abstract base class for forward dataflow analysis over a <see cref="ControlFlowGraph" />.
/// </summary>
/// <typeparam name="TBlockAnalysisData">
///     The type representing the analysis state (lattice element) at each basic block.
///     Must support equality comparison via <see cref="IsEqual" /> and a join operation via <see cref="Merge" />.
/// </typeparam>
/// <remarks>
///     <para>
///         Implements a standard worklist-based forward dataflow analysis algorithm:
///     </para>
///     <list type="number">
///         <item>
///             <description>Compute reverse post-order via DFS from the entry block.</description>
///         </item>
///         <item>
///             <description>Initialize all blocks with <see cref="GetEmptyAnalysisData" /> (lattice bottom).</description>
///         </item>
///         <item>
///             <description>Seed the worklist with all blocks in reverse post-order.</description>
///         </item>
///         <item>
///             <description>
///                 While the worklist is non-empty: dequeue a block, merge predecessor outputs,
///                 run the transfer function, and re-enqueue successors if the output changed.
///             </description>
///         </item>
///     </list>
///     <para>
///         Subclasses implement the lattice operations (<see cref="GetEmptyAnalysisData" />,
///         <see cref="Merge" />, <see cref="IsEqual" />) and the transfer function
///         (<see cref="AnalyzeBlock" />, <see cref="AnalyzeNonConditionalBranch" />,
///         <see cref="AnalyzeConditionalBranch" />).
///     </para>
///     <para>
///         Typical uses include null-state tracking, resource disposal verification,
///         definite assignment analysis, and taint propagation.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // A simple "is definitely assigned" dataflow analysis
/// public class DefiniteAssignmentAnalyzer : DataFlowAnalyzer&lt;HashSet&lt;ILocalSymbol&gt;&gt;
/// {
///     protected override HashSet&lt;ILocalSymbol&gt; GetEmptyAnalysisData()
///         =&gt; new(SymbolEqualityComparer.Default);
///
///     protected override HashSet&lt;ILocalSymbol&gt; Merge(
///         HashSet&lt;ILocalSymbol&gt; data1,
///         HashSet&lt;ILocalSymbol&gt; data2,
///         CancellationToken ct)
///     {
///         // Intersection = definitely assigned on ALL paths
///         var result = new HashSet&lt;ILocalSymbol&gt;(data1, SymbolEqualityComparer.Default);
///         result.IntersectWith(data2);
///         return result;
///     }
///
///     protected override bool IsEqual(
///         HashSet&lt;ILocalSymbol&gt; data1,
///         HashSet&lt;ILocalSymbol&gt; data2)
///         =&gt; data1.SetEquals(data2);
///
///     // ... implement remaining abstract members
/// }
/// </code>
/// </example>
/// <seealso cref="ControlFlowGraph" />
/// <seealso cref="BasicBlock" />
/// <seealso cref="ControlFlowBranch" />
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    abstract class DataFlowAnalyzer<TBlockAnalysisData>
{
    /// <summary>
    ///     Gets a value indicating whether unreachable blocks should be included in the analysis.
    /// </summary>
    /// <remarks>
    ///     When <c>false</c> (default), only blocks reachable from the entry block are analyzed.
    ///     Set to <c>true</c> to also analyze blocks that are not reachable (e.g., for dead code detection).
    /// </remarks>
    protected virtual bool AnalyzeUnreachableBlocks => false;

    /// <summary>
    ///     Creates the initial (bottom) analysis data for the lattice.
    /// </summary>
    /// <returns>
    ///     An instance of <typeparamref name="TBlockAnalysisData" /> representing the initial state
    ///     before any analysis has occurred.
    /// </returns>
    protected abstract TBlockAnalysisData GetEmptyAnalysisData();

    /// <summary>
    ///     Gets the current analysis data for a given basic block.
    /// </summary>
    /// <param name="block">The basic block to retrieve data for.</param>
    /// <returns>The current analysis state for <paramref name="block" />.</returns>
    protected abstract TBlockAnalysisData GetCurrentAnalysisData(BasicBlock block);

    /// <summary>
    ///     Sets the analysis data for a given basic block.
    /// </summary>
    /// <param name="block">The basic block to update.</param>
    /// <param name="data">The new analysis data to associate with the block.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    protected abstract void SetCurrentAnalysisData(BasicBlock block, TBlockAnalysisData data, CancellationToken cancellationToken);

    /// <summary>
    ///     Applies the transfer function to a basic block, computing the analysis data at its exit
    ///     from the data at its entry.
    /// </summary>
    /// <param name="block">The block to analyze.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The analysis data after processing all operations in <paramref name="block" />.</returns>
    protected abstract TBlockAnalysisData AnalyzeBlock(BasicBlock block, CancellationToken cancellationToken);

    /// <summary>
    ///     Computes the analysis data to propagate along a non-conditional branch.
    /// </summary>
    /// <param name="block">The source block of the branch.</param>
    /// <param name="currentAnalysisData">The analysis data at the exit of <paramref name="block" />.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The analysis data to propagate to the branch's destination block.</returns>
    protected abstract TBlockAnalysisData AnalyzeNonConditionalBranch(
        BasicBlock block,
        TBlockAnalysisData currentAnalysisData,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Computes the analysis data to propagate along both branches of a conditional branch.
    /// </summary>
    /// <param name="block">The source block containing the conditional branch.</param>
    /// <param name="currentAnalysisData">The analysis data at the exit of <paramref name="block" />.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>
    ///     A tuple of (whenTrue, whenFalse) analysis data for the true and false branches respectively.
    /// </returns>
    protected abstract (TBlockAnalysisData whenTrue, TBlockAnalysisData whenFalse) AnalyzeConditionalBranch(
        BasicBlock block,
        TBlockAnalysisData currentAnalysisData,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Computes the join (least upper bound) of two analysis data values at a merge point.
    /// </summary>
    /// <param name="data1">The first analysis data.</param>
    /// <param name="data2">The second analysis data.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The merged analysis data representing the join of both inputs.</returns>
    protected abstract TBlockAnalysisData Merge(
        TBlockAnalysisData data1,
        TBlockAnalysisData data2,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Determines whether two analysis data values are equal (for fixed-point detection).
    /// </summary>
    /// <param name="data1">The first analysis data.</param>
    /// <param name="data2">The second analysis data.</param>
    /// <returns>
    ///     <c>true</c> if <paramref name="data1" /> and <paramref name="data2" /> represent the same
    ///     analysis state; otherwise <c>false</c>.
    /// </returns>
    protected abstract bool IsEqual(TBlockAnalysisData data1, TBlockAnalysisData data2);

    /// <summary>
    ///     Runs the forward dataflow analysis on the given control flow graph.
    /// </summary>
    /// <param name="cfg">The control flow graph to analyze.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <remarks>
    ///     <para>
    ///         After this method completes, the analysis data for each block can be retrieved
    ///         via <see cref="GetCurrentAnalysisData" />.
    ///     </para>
    /// </remarks>
    public void RunAnalysis(ControlFlowGraph cfg, CancellationToken cancellationToken)
    {
        Guard.NotNull(cfg);

        var blocks = cfg.Blocks;
        if (blocks.IsEmpty)
        {
            return;
        }

        // Step 1: Compute reverse post-order via iterative DFS
        var rpo = ComputeReversePostOrder(blocks, cancellationToken);

        // Step 2: Initialize all blocks with empty analysis data
        foreach (var block in blocks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SetCurrentAnalysisData(block, GetEmptyAnalysisData(), cancellationToken);
        }

        // Step 3: Seed worklist with blocks in RPO order
        var inWorklist = new bool[blocks.Length];
        var worklist = new Queue<BasicBlock>();

        foreach (var block in rpo)
        {
            if (AnalyzeUnreachableBlocks || block.IsReachable)
            {
                worklist.Enqueue(block);
                inWorklist[block.Ordinal] = true;
            }
        }

        // Step 4: Fixed-point iteration (push-based)
        // Stored data per block = accumulated entry state from predecessors.
        // AnalyzeBlock reads it via GetCurrentAnalysisData, returns exit state.
        // Exit state is propagated to successors and merged into their entry state.
        while (worklist.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var block = worklist.Dequeue();
            inWorklist[block.Ordinal] = false;

            // Run transfer function: reads block's entry state, returns exit state
            var blockOutput = AnalyzeBlock(block, cancellationToken);

            // Propagate exit state to successors
            if (block.ConditionalSuccessor != null && block.FallThroughSuccessor != null)
            {
                // Conditional branch: split into whenTrue/whenFalse
                var (whenTrue, whenFalse) = AnalyzeConditionalBranch(block, blockOutput, cancellationToken);

                // ConditionKind is on the BasicBlock, not the branch — check it to map correctly
                bool conditionIsWhenTrue = block.ConditionKind == ControlFlowConditionKind.WhenTrue;
                var conditionalData = conditionIsWhenTrue ? whenTrue : whenFalse;
                var fallThroughData = conditionIsWhenTrue ? whenFalse : whenTrue;

                MergeAndEnqueue(block.ConditionalSuccessor.Destination, conditionalData, worklist, inWorklist, cancellationToken);
                MergeAndEnqueue(block.FallThroughSuccessor.Destination, fallThroughData, worklist, inWorklist, cancellationToken);
            }
            else if (block.FallThroughSuccessor != null)
            {
                var data = AnalyzeNonConditionalBranch(block, blockOutput, cancellationToken);
                MergeAndEnqueue(block.FallThroughSuccessor.Destination, data, worklist, inWorklist, cancellationToken);
            }
            else if (block.ConditionalSuccessor != null)
            {
                var data = AnalyzeNonConditionalBranch(block, blockOutput, cancellationToken);
                MergeAndEnqueue(block.ConditionalSuccessor.Destination, data, worklist, inWorklist, cancellationToken);
            }
        }
    }

    private void MergeAndEnqueue(
        BasicBlock? destination,
        TBlockAnalysisData data,
        Queue<BasicBlock> worklist,
        bool[] inWorklist,
        CancellationToken cancellationToken)
    {
        if (destination == null)
        {
            return;
        }

        if (!AnalyzeUnreachableBlocks && !destination.IsReachable)
        {
            return;
        }

        var currentData = GetCurrentAnalysisData(destination);
        var mergedData = Merge(currentData, data, cancellationToken);

        if (!IsEqual(currentData, mergedData))
        {
            SetCurrentAnalysisData(destination, mergedData, cancellationToken);
            if (!inWorklist[destination.Ordinal])
            {
                worklist.Enqueue(destination);
                inWorklist[destination.Ordinal] = true;
            }
        }
    }

    private static ImmutableArray<BasicBlock> ComputeReversePostOrder(
        ImmutableArray<BasicBlock> blocks,
        CancellationToken cancellationToken)
    {
        var visited = new bool[blocks.Length];
        var postOrder = new List<BasicBlock>(blocks.Length);

        // Iterative DFS using an explicit stack to avoid stack overflow on deep CFGs
        var dfsStack = new Stack<(BasicBlock block, bool processed)>();
        dfsStack.Push((blocks[0], false));

        while (dfsStack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (block, processed) = dfsStack.Pop();

            if (processed)
            {
                postOrder.Add(block);
                continue;
            }

            if (visited[block.Ordinal])
            {
                continue;
            }

            visited[block.Ordinal] = true;

            // Push this block again marked as processed (will be added to post-order after children)
            dfsStack.Push((block, true));

            // Push successors (in reverse to maintain ordering)
            var conditional = block.ConditionalSuccessor?.Destination;
            var fallThrough = block.FallThroughSuccessor?.Destination;

            if (conditional != null && !visited[conditional.Ordinal])
            {
                dfsStack.Push((conditional, false));
            }

            if (fallThrough != null && !visited[fallThrough.Ordinal])
            {
                dfsStack.Push((fallThrough, false));
            }
        }

        // Reverse to get RPO
        postOrder.Reverse();
        return postOrder.ToImmutableArray();
    }
}
