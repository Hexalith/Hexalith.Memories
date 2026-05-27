namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Server.Search;

using Shouldly;

public class ScoreNormalizerTests
{
    // --- NormalizeBm25 Tests ---

    [Fact]
    public void NormalizeBm25_RawScoreZero_ShouldReturnZero()
    {
        double result = ScoreNormalizer.NormalizeBm25(0.0, 1000, 200.0);

        result.ShouldBe(0.0);
    }

    [Fact]
    public void NormalizeBm25_KnownInputs_ShouldReturnExpectedValue()
    {
        // k = log2(1001) * (200/100) = ~9.967 * 2 = ~19.934
        // normalized = 5.0 / (5.0 + 19.934) = ~0.2006
        double result = ScoreNormalizer.NormalizeBm25(5.0, 1000, 200.0);

        result.ShouldBe(0.2006, tolerance: 0.01);
    }

    [Fact]
    public void NormalizeBm25_VeryHighRawScore_ShouldSaturateNearOne()
    {
        double result = ScoreNormalizer.NormalizeBm25(100.0, 1000, 200.0);

        result.ShouldBeGreaterThan(0.8);
        result.ShouldBeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void NormalizeBm25_AnyPositiveRawScore_ShouldBeInRange()
    {
        double[] rawScores = [0.001, 0.1, 1.0, 5.0, 10.0, 50.0, 100.0, 1000.0, 10000.0];
        foreach (double rawScore in rawScores)
        {
            double result = ScoreNormalizer.NormalizeBm25(rawScore, 500, 150.0);

            result.ShouldBeInRange(0.0, 1.0, $"rawScore={rawScore}");
        }
    }

    [Fact]
    public void NormalizeBm25_DocumentCountZero_ShouldReturnZero()
    {
        double result = ScoreNormalizer.NormalizeBm25(5.0, 0, 200.0);

        result.ShouldBe(0.0);
    }

    [Fact]
    public void NormalizeBm25_Monotonicity_HigherRawScoreProducesHigherNormalized()
    {
        double low = ScoreNormalizer.NormalizeBm25(1.0, 1000, 200.0);
        double mid = ScoreNormalizer.NormalizeBm25(5.0, 1000, 200.0);
        double high = ScoreNormalizer.NormalizeBm25(20.0, 1000, 200.0);

        low.ShouldBeLessThan(mid);
        mid.ShouldBeLessThan(high);
    }

    [Fact]
    public void NormalizeBm25_NegativeRawScore_ShouldReturnZero()
    {
        double result = ScoreNormalizer.NormalizeBm25(-5.0, 1000, 200.0);

        result.ShouldBe(0.0);
    }

    [Fact]
    public void NormalizeBm25_NaN_ShouldReturnZero()
    {
        double result = ScoreNormalizer.NormalizeBm25(double.NaN, 1000, 200.0);

        result.ShouldBe(0.0);
    }

    [Fact]
    public void NormalizeBm25_PositiveInfinity_ShouldReturnZero()
    {
        double result = ScoreNormalizer.NormalizeBm25(double.PositiveInfinity, 1000, 200.0);

        result.ShouldBe(0.0);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void NormalizeBm25_NonFiniteAverageDocumentLength_ShouldReturnZero(double averageDocumentLength)
    {
        double result = ScoreNormalizer.NormalizeBm25(5.0, 1000, averageDocumentLength);

        result.ShouldBe(0.0);
    }

    [Fact]
    public void NormalizeBm25_MaxDocumentCount_ShouldRemainFinite()
    {
        double result = ScoreNormalizer.NormalizeBm25(5.0, int.MaxValue, 200.0);

        double.IsFinite(result).ShouldBeTrue();
        result.ShouldBe(5.0 / 67.0, tolerance: 0.0001);
    }

    [Fact]
    public void NormalizeBm25_TinyCorpus_ShouldUseComputedSaturationConstant()
    {
        double result = ScoreNormalizer.NormalizeBm25(0.5, 1, 1.0);

        result.ShouldBe(0.5 / 0.51, tolerance: 0.0001);
    }

    // --- NormalizeCosine Tests ---

    [Fact]
    public void NormalizeCosine_ValidScore_ShouldPassthrough()
    {
        double result = ScoreNormalizer.NormalizeCosine(0.91);

        result.ShouldBe(0.91);
    }

    [Fact]
    public void NormalizeCosine_Zero_ShouldReturnZero()
    {
        double result = ScoreNormalizer.NormalizeCosine(0.0);

        result.ShouldBe(0.0);
    }

    [Fact]
    public void NormalizeCosine_One_ShouldReturnOne()
    {
        double result = ScoreNormalizer.NormalizeCosine(1.0);

        result.ShouldBe(1.0);
    }

    [Fact]
    public void NormalizeCosine_FloatingPointOvershoot_ShouldClampToOne()
    {
        double result = ScoreNormalizer.NormalizeCosine(1.001);

        result.ShouldBe(1.0);
    }

    [Fact]
    public void NormalizeCosine_NaN_ShouldReturnZero()
    {
        double result = ScoreNormalizer.NormalizeCosine(double.NaN);

        result.ShouldBe(0.0);
    }

    // --- NormalizeGraphProximity Tests ---

    [Fact]
    public void NormalizeGraphProximity_HopZero_ShouldReturnOne()
    {
        double result = ScoreNormalizer.NormalizeGraphProximity(0);

        result.ShouldBe(1.0);
    }

    [Fact]
    public void NormalizeGraphProximity_HopOne_ShouldReturnHalf()
    {
        double result = ScoreNormalizer.NormalizeGraphProximity(1);

        result.ShouldBe(0.5);
    }

    [Fact]
    public void NormalizeGraphProximity_HopTwo_ShouldReturnOneThird()
    {
        double result = ScoreNormalizer.NormalizeGraphProximity(2);

        result.ShouldBe(1.0 / 3.0, tolerance: 0.001);
    }

    [Fact]
    public void NormalizeGraphProximity_HopThree_ShouldReturnQuarter()
    {
        double result = ScoreNormalizer.NormalizeGraphProximity(3);

        result.ShouldBe(0.25);
    }

    [Fact]
    public void NormalizeGraphProximity_VeryLargeHop_ShouldBePositiveAndLessThanOne()
    {
        double result = ScoreNormalizer.NormalizeGraphProximity(1000);

        result.ShouldBeGreaterThan(0.0);
        result.ShouldBeLessThan(1.0);
    }

    [Fact]
    public void NormalizeGraphProximity_NegativeHop_ShouldThrowArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ScoreNormalizer.NormalizeGraphProximity(-1));
    }
}
