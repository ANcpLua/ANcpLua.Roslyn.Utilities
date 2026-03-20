namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     Pure statistical math functions: entropy, KL divergence, Box-Cox transforms, Z-scores,
///     Laplace smoothing, and Reciprocal Rank Fusion scoring.
///     All self-contained numerical algorithms with no external dependencies.
/// </summary>
#if ANCPLUA_ROSLYN_PUBLIC
public
#else
internal
#endif
    static class StatisticalMath
{
    // =========================================================================
    // Convex Analysis
    // =========================================================================

    /// <summary>
    ///     Elementwise function for computing entropy: <c>-x * ln(x)</c>.
    /// </summary>
    /// <param name="x">The input value.</param>
    /// <returns>
    ///     <c>-x * ln(x)</c> when <paramref name="x" /> is positive,
    ///     <c>0</c> when zero, <see cref="double.NegativeInfinity" /> when negative,
    ///     or <see cref="double.NaN" /> when NaN.
    /// </returns>
    public static double Entr(double x) =>
        double.IsNaN(x) ? x :
        x > 0 ? -x * System.Math.Log(x) :
        x == 0 ? 0 :
        double.NegativeInfinity;

    /// <summary>
    ///     Relative entropy: <c>x * ln(x / y)</c>.
    /// </summary>
    /// <param name="x">The first distribution value.</param>
    /// <param name="y">The second distribution value.</param>
    /// <returns>
    ///     <c>x * ln(x / y)</c> when both are positive,
    ///     <c>0</c> when <paramref name="x" /> is zero and <paramref name="y" /> is non-negative,
    ///     or <see cref="double.PositiveInfinity" /> otherwise.
    /// </returns>
    public static double RelEntr(double x, double y) =>
        double.IsNaN(x) || double.IsNaN(y) ? double.NaN :
        x > 0 && y > 0 ? x * System.Math.Log(x / y) :
        x == 0 && y >= 0 ? 0 :
        double.PositiveInfinity;

    // =========================================================================
    // Probability & Information Theory
    // =========================================================================

    /// <summary>
    ///     Laplace smooth a probability distribution to remove zeros while
    ///     preserving relative proportions.
    /// </summary>
    /// <param name="probabilities">The raw probability values.</param>
    /// <param name="alpha">The smoothing parameter (default 1e-3).</param>
    /// <returns>A new array containing the smoothed distribution.</returns>
    public static double[] LaplaceSmooth(ReadOnlySpan<double> probabilities, double alpha = 1e-3)
    {
        double total = 0;
        for (var i = 0; i < probabilities.Length; i++)
            total += probabilities[i];

        var result = new double[probabilities.Length];
        var denominator = total + (alpha * probabilities.Length);
        for (var i = 0; i < probabilities.Length; i++)
            result[i] = (probabilities[i] + alpha) / denominator;

        return result;
    }

    /// <summary>
    ///     Shannon entropy: <c>H = -Sum p(x) log(p(x))</c>.
    ///     Input is normalized to sum to 1.
    /// </summary>
    /// <param name="xs">The distribution values.</param>
    /// <returns>The Shannon entropy of the distribution.</returns>
    public static double Entropy(ReadOnlySpan<double> xs)
    {
        double total = 0;
        for (var i = 0; i < xs.Length; i++)
            total += xs[i];

        if (total == 0) return 0;

        double result = 0;
        for (var i = 0; i < xs.Length; i++)
            result += Entr(xs[i] / total);

        return result;
    }

    /// <summary>
    ///     Elementwise relative entropy between two distributions.
    ///     Only non-zero elements of <paramref name="a" /> contribute to the result.
    /// </summary>
    /// <param name="a">The first distribution.</param>
    /// <param name="b">The second distribution.</param>
    /// <returns>An array of relative entropy values for non-zero elements.</returns>
    /// <exception cref="ArgumentException">Thrown when distribution lengths differ.</exception>
    public static double[] RelativeEntropy(ReadOnlySpan<double> a, ReadOnlySpan<double> b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Mismatched distribution lengths");

        var result = new double[a.Length];
        var idx = 0;
        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] != 0)
                result[idx++] = RelEntr(a[i], b[i]);
        }

        return result.AsSpan(0, idx).ToArray();
    }

    /// <summary>
    ///     Kullback-Leibler divergence: <c>D_KL(a || b) = Sum rel_entr(a_i, b_i)</c>.
    /// </summary>
    /// <param name="a">The first distribution.</param>
    /// <param name="b">The second distribution.</param>
    /// <returns>The KL divergence from <paramref name="a" /> to <paramref name="b" />.</returns>
    public static double KlDivergence(ReadOnlySpan<double> a, ReadOnlySpan<double> b)
    {
        var re = RelativeEntropy(a, b);
        double sum = 0;
        for (var i = 0; i < re.Length; i++)
            sum += re[i];
        return sum;
    }

    // =========================================================================
    // Ranking & Fusion
    // =========================================================================

    /// <summary>
    ///     Reciprocal Rank Fusion score combining entropy and KL rankings.
    /// </summary>
    /// <param name="entropyScores">Per-key entropy scores.</param>
    /// <param name="klScores">Per-key KL divergence scores.</param>
    /// <param name="entropyAlpha">Weight for entropy ranking (must sum to 1 with <paramref name="klAlpha" />).</param>
    /// <param name="klAlpha">Weight for KL ranking (must sum to 1 with <paramref name="entropyAlpha" />).</param>
    /// <param name="offset">RRF offset constant (default 60).</param>
    /// <returns>An array of fused RRF scores.</returns>
    /// <exception cref="ArgumentException">Thrown when alphas do not sum to 1.</exception>
    public static double[] RrfScore(
        ReadOnlySpan<double> entropyScores,
        ReadOnlySpan<double> klScores,
        double entropyAlpha = 0.2,
        double klAlpha = 0.8,
        int offset = 60)
    {
        var alphaSum = entropyAlpha + klAlpha - 1.0;
        if (!(double.IsNaN(alphaSum) || double.IsInfinity(alphaSum) || alphaSum == 0.0)
            && System.Math.Abs(alphaSum) > 1e-9)
            throw new ArgumentException("Entropy alpha and KL alpha must sum to 1.");

        var entropyRanks = RankMin(entropyScores, true);
        var klRanks = RankMin(klScores);

        var result = new double[entropyScores.Length];
        for (var i = 0; i < result.Length; i++)
        {
            var a = klAlpha * (1.0 / (offset + klRanks[i]));
            var b = entropyAlpha * (1.0 / (offset + entropyRanks[i]));
            result[i] = a + b;
        }

        return result;
    }

    /// <summary>
    ///     Assign dense ranks to values using min-rank strategy.
    /// </summary>
    /// <param name="xs">The values to rank.</param>
    /// <param name="ascending">
    ///     When <c>true</c>, ranks in ascending order; when <c>false</c>, ranks in descending order.
    /// </param>
    /// <returns>An array of integer ranks corresponding to each input value.</returns>
    public static int[] RankMin(ReadOnlySpan<double> xs, bool ascending = false)
    {
        var sorted = new SortedSet<double>(xs.ToArray());
        var ranks = new Dictionary<double, int>();
        var rank = 1;

        var ordered = ascending
            ? sorted
            : sorted.Reverse();

        foreach (var val in ordered)
            ranks[val] = rank++;

        var result = new int[xs.Length];
        for (var i = 0; i < xs.Length; i++)
            result[i] = ranks[xs[i]];

        return result;
    }

    // =========================================================================
    // Box-Cox Transform
    // =========================================================================

    /// <summary>
    ///     Apply Box-Cox transformation. If <paramref name="lambdaParam" /> is null,
    ///     finds the optimal lambda via MLE with ternary search.
    /// </summary>
    /// <param name="values">The values to transform.</param>
    /// <param name="lambdaParam">
    ///     An explicit lambda parameter. When <c>null</c>, the optimal lambda is found automatically.
    /// </param>
    /// <returns>A tuple of the transformed values and the lambda used.</returns>
    public static (double[] Transformed, double Lambda) BoxCoxTransform(
        ReadOnlySpan<double> values, double? lambdaParam = null)
    {
        if (values.IsEmpty)
            return ([], lambdaParam ?? 0.0);

        var minValue = double.MaxValue;
        for (var i = 0; i < values.Length; i++)
        {
            if (values[i] < minValue)
                minValue = values[i];
        }

        Span<double> shifted = stackalloc double[values.Length];
        if (minValue <= 0)
        {
            var shiftAmount = -minValue + 1e-10;
            for (var i = 0; i < values.Length; i++)
                shifted[i] = values[i] + shiftAmount;
        }
        else
        {
            values.CopyTo(shifted);
        }

        var lambda = lambdaParam ?? BoxCoxNormMax(shifted);

        var transformed = new double[values.Length];
        if (lambda == 0.0)
        {
            for (var i = 0; i < shifted.Length; i++)
                transformed[i] = System.Math.Log(System.Math.Max(shifted[i], 1e-10));
        }
        else
        {
            for (var i = 0; i < shifted.Length; i++)
                transformed[i] = (System.Math.Pow(System.Math.Max(shifted[i], 1e-10), lambda) - 1) / lambda;
        }

        return (transformed, lambda);
    }

    /// <summary>
    ///     Box-Cox log-likelihood function using numerically stable log-space arithmetic.
    /// </summary>
    /// <param name="lambdaParam">The lambda parameter to evaluate.</param>
    /// <param name="values">The positive values to compute the log-likelihood for.</param>
    /// <returns>The Box-Cox log-likelihood value.</returns>
    internal static double BoxCoxLlf(double lambdaParam, ReadOnlySpan<double> values)
    {
        var n = values.Length;
        if (n == 0) return 0.0;

        double logSum = 0;
        Span<double> logValues = stackalloc double[n];
        for (var i = 0; i < n; i++)
        {
            logValues[i] = System.Math.Log(System.Math.Max(values[i], 1e-10));
            logSum += logValues[i];
        }

        double logvar;
        if (lambdaParam == 0.0)
        {
            var logMean = logSum / n;
            double logVar = 0;
            for (var i = 0; i < n; i++)
                logVar += (logValues[i] - logMean) * (logValues[i] - logMean);
            logVar /= n;
            logvar = System.Math.Log(System.Math.Max(logVar, 1e-10));
        }
        else
        {
            Span<double> logx = stackalloc double[n];
            double logxMean = 0;
            for (var i = 0; i < n; i++)
            {
                logx[i] = lambdaParam * logValues[i];
                logxMean += logx[i];
            }

            logxMean /= n;
            double logxVar = 0;
            for (var i = 0; i < n; i++)
                logxVar += (logx[i] - logxMean) * (logx[i] - logxMean);
            logxVar /= n;
            logvar = System.Math.Log(System.Math.Max(logxVar, 1e-10)) - (2 * System.Math.Log(System.Math.Abs(lambdaParam)));
        }

        return ((lambdaParam - 1) * logSum) - (n / 2.0 * logvar);
    }

    /// <summary>
    ///     Find optimal Box-Cox lambda via MLE with ternary search over [-2, 2].
    /// </summary>
    /// <param name="values">The positive values to optimize for.</param>
    /// <param name="maxIters">Maximum number of ternary search iterations.</param>
    /// <returns>The optimal lambda value.</returns>
    internal static double BoxCoxNormMax(ReadOnlySpan<double> values, int maxIters = 100)
    {
        if (values.IsEmpty) return 0.0;

        var left = -2.0;
        var right = 2.0;
        const double tolerance = 1e-6;

        for (var i = 0; i < maxIters && right - left > tolerance; i++)
        {
            var m1 = left + ((right - left) / 3);
            var m2 = right - ((right - left) / 3);

            if (BoxCoxLlf(m1, values) > BoxCoxLlf(m2, values))
                right = m2;
            else
                left = m1;
        }

        return (left + right) / 2;
    }

    // =========================================================================
    // Z-Scores
    // =========================================================================

    /// <summary>
    ///     Calculate Z-scores for a list of values: <c>(x - mean) / stdDev</c>.
    /// </summary>
    /// <param name="values">The values to standardize.</param>
    /// <returns>
    ///     An array of Z-scores, or an empty array when input is empty,
    ///     or all zeros when standard deviation is zero.
    /// </returns>
    public static double[] CalculateZScores(ReadOnlySpan<double> values)
    {
        if (values.IsEmpty) return [];

        double sum = 0;
        for (var i = 0; i < values.Length; i++)
            sum += values[i];

        var mean = sum / values.Length;

        double variance = 0;
        for (var i = 0; i < values.Length; i++)
            variance += (values[i] - mean) * (values[i] - mean);
        variance /= values.Length;

        var stdDev = System.Math.Sqrt(variance);
        if (stdDev == 0)
        {
            var zeros = new double[values.Length];
            return zeros;
        }

        var result = new double[values.Length];
        for (var i = 0; i < values.Length; i++)
            result[i] = (values[i] - mean) / stdDev;

        return result;
    }
}
