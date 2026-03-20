namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Compares multi-dimensional keyed distributions using KL divergence,
///     Shannon entropy, and Reciprocal Rank Fusion scoring.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class DistributionComparer
{
    /// <summary>
    ///     KL-score a multi-dimensional distribution. Returns keys sorted by descending KL divergence.
    /// </summary>
    /// <param name="baseline">Baseline distribution observations.</param>
    /// <param name="outliers">Outlier distribution observations.</param>
    /// <param name="totalBaseline">Total count of baseline observations.</param>
    /// <param name="totalOutliers">Total count of outlier observations.</param>
    /// <returns>Keys with their KL divergence scores, sorted descending.</returns>
    public static List<KeyScore> KeyedKlScore(
        KeyedValueCount[] baseline,
        KeyedValueCount[] outliers,
        int totalBaseline,
        int totalOutliers)
    {
        var results = new List<KeyScore>();
        foreach (var (key, a, b) in GenNormalizedDistributions(baseline, outliers, totalBaseline, totalOutliers))
            results.Add(new KeyScore(key, StatisticalMath.KlDivergence(a, b)));

        results.Sort(static (x, y) => y.Score.CompareTo(x.Score));
        return results;
    }

    /// <summary>
    ///     RRF-score a multi-dimensional distribution combining entropy and KL rankings.
    /// </summary>
    /// <param name="baseline">Baseline distribution observations.</param>
    /// <param name="outliers">Outlier distribution observations.</param>
    /// <param name="totalBaseline">Total count of baseline observations.</param>
    /// <param name="totalOutliers">Total count of outlier observations.</param>
    /// <param name="entropyAlpha">Weight for entropy ranking (must sum to 1 with <paramref name="klAlpha" />).</param>
    /// <param name="klAlpha">Weight for KL ranking (must sum to 1 with <paramref name="entropyAlpha" />).</param>
    /// <param name="offset">RRF offset constant (default 60).</param>
    /// <returns>Keys with their fused RRF scores, sorted descending.</returns>
    public static List<KeyScore> KeyedRrfScore(
        KeyedValueCount[] baseline,
        KeyedValueCount[] outliers,
        int totalBaseline,
        int totalOutliers,
        double entropyAlpha = 0.2,
        double klAlpha = 0.8,
        int offset = 60)
    {
        List<string> keys = [];
        List<double> entropyScores = [];
        List<double> klScores = [];

        foreach (var (key, a, b) in GenNormalizedDistributions(baseline, outliers, totalBaseline, totalOutliers))
        {
            keys.Add(key);
            entropyScores.Add(StatisticalMath.Entropy(b));
            klScores.Add(StatisticalMath.KlDivergence(a, b));
        }

        double[] eArr = [.. entropyScores];
        double[] klArr = [.. klScores];
        var rrfScores = StatisticalMath.RrfScore(eArr, klArr, entropyAlpha, klAlpha, offset);

        var results = new List<KeyScore>(keys.Count);
        for (var i = 0; i < keys.Count; i++)
            results.Add(new KeyScore(keys[i], rrfScores[i]));

        results.Sort(static (x, y) => y.Score.CompareTo(x.Score));
        return results;
    }

    /// <summary>
    ///     RRF-score with Box-Cox + Z-score filtering. Keys below the Z threshold
    ///     on both entropy and KL are marked as filtered.
    /// </summary>
    /// <param name="baseline">Baseline distribution observations.</param>
    /// <param name="outliers">Outlier distribution observations.</param>
    /// <param name="totalBaseline">Total count of baseline observations.</param>
    /// <param name="totalOutliers">Total count of outlier observations.</param>
    /// <param name="entropyAlpha">Weight for entropy ranking.</param>
    /// <param name="klAlpha">Weight for KL ranking.</param>
    /// <param name="offset">RRF offset constant.</param>
    /// <param name="zThreshold">Z-score threshold below which both scores must fall for a key to be filtered.</param>
    /// <returns>Keys with their fused RRF scores and filter flags, sorted descending.</returns>
    public static List<KeyScoreFiltered> KeyedRrfScoreWithFilter(
        KeyedValueCount[] baseline,
        KeyedValueCount[] outliers,
        int totalBaseline,
        int totalOutliers,
        double entropyAlpha = 0.2,
        double klAlpha = 0.8,
        int offset = 60,
        double zThreshold = 1.5)
    {
        List<string> keys = [];
        List<double> entropyScores = [];
        List<double> klScores = [];

        foreach (var (key, a, b) in GenNormalizedDistributions(baseline, outliers, totalBaseline, totalOutliers))
        {
            keys.Add(key);
            entropyScores.Add(StatisticalMath.Entropy(b));
            klScores.Add(StatisticalMath.KlDivergence(a, b));
        }

        double[] eArr = [.. entropyScores];
        double[] klArr = [.. klScores];
        var (normalizedEntropy, _) = StatisticalMath.BoxCoxTransform(eArr);
        var (normalizedKl, _) = StatisticalMath.BoxCoxTransform(klArr);
        var entropyZ = StatisticalMath.CalculateZScores(normalizedEntropy);
        var klZ = StatisticalMath.CalculateZScores(normalizedKl);
        var rrfScores = StatisticalMath.RrfScore(eArr, klArr, entropyAlpha, klAlpha, offset);

        var results = new List<KeyScoreFiltered>(keys.Count);
        for (var i = 0; i < keys.Count; i++)
        {
            var filtered = entropyZ[i] <= zThreshold && klZ[i] <= zThreshold;
            results.Add(new KeyScoreFiltered(keys[i], rrfScores[i], filtered));
        }

        results.Sort(static (x, y) => y.Score.CompareTo(x.Score));
        return results;
    }

    // =========================================================================
    // Internal distribution normalization pipeline
    // =========================================================================

    private static List<(string Key, double[] Baseline, double[] Outliers)>
        GenNormalizedDistributions(
            KeyedValueCount[] baseline,
            KeyedValueCount[] outliers,
            int totalBaseline,
            int totalOutliers)
    {
        var keyedBaseline = ToAttributeDict(baseline);
        var keyedOutliers = ToAttributeDict(outliers);
        var results = new List<(string, double[], double[])>();

        foreach (var key in keyedOutliers.Keys)
        {
            if (!keyedBaseline.TryGetValue(key, out var baselineDist))
                continue;

            var outliersDist = keyedOutliers[key];

            AddUnseenValue(baselineDist, totalBaseline);
            AddUnseenValue(outliersDist, totalOutliers);
            EnsureSymmetry(baselineDist, outliersDist);

            var smoothedBaseline = StatisticalMath.LaplaceSmooth(
                [.. baselineDist.Values]);
            var smoothedOutliers = StatisticalMath.LaplaceSmooth(
                [.. outliersDist.Values]);

            results.Add((key, smoothedBaseline, smoothedOutliers));
        }

        return results;
    }

    private static Dictionary<string, Dictionary<string, double>> ToAttributeDict(
        KeyedValueCount[] rows)
    {
        var result = new Dictionary<string, Dictionary<string, double>>();
        foreach (ref readonly var row in rows.AsSpan())
        {
            if (!result.TryGetValue(row.Key, out var dict))
                result[row.Key] = dict = [];
            dict[row.Value] = row.Count;
        }

        return result;
    }

    private static void AddUnseenValue(Dictionary<string, double> dist, int total)
    {
        double count = 0;
        foreach (var v in dist.Values)
            count += v;

        var delta = total - count;
        if (delta > 0)
            dist[""] = delta;
    }

    private static void EnsureSymmetry(
        Dictionary<string, double> a, Dictionary<string, double> b)
    {
        foreach (var key in b.Keys)
        {
            if (!a.ContainsKey(key)) a[key] = 0;
        }

        foreach (var key in a.Keys.ToArray())
        {
            if (!b.ContainsKey(key)) b[key] = 0;
        }
    }

    /// <summary>Key, value, count triple representing one observation.</summary>
#if ANCPLUA_ROSLYN_PUBLIC
    public
#else
    internal
#endif
        readonly record struct KeyedValueCount(string Key, string Value, double Count);

    /// <summary>Key with its score.</summary>
#if ANCPLUA_ROSLYN_PUBLIC
    public
#else
    internal
#endif
        readonly record struct KeyScore(string Key, double Score);

    /// <summary>Key with score and filter flag.</summary>
#if ANCPLUA_ROSLYN_PUBLIC
    public
#else
    internal
#endif
        readonly record struct KeyScoreFiltered(string Key, double Score, bool Filtered);
}
