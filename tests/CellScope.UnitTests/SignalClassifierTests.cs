using CellScope.Domain.Enums;
using CellScope.Domain.Services;
using FluentAssertions;
using Xunit;

namespace CellScope.UnitTests;

public class SignalClassifierTests
{
    [Theory]
    [InlineData(-60, SignalQualityRating.Excellent)]
    [InlineData(-70, SignalQualityRating.Excellent)]
    [InlineData(-75, SignalQualityRating.Good)]
    [InlineData(-85, SignalQualityRating.Good)]
    [InlineData(-92, SignalQualityRating.Fair)]
    [InlineData(-100, SignalQualityRating.Fair)]
    [InlineData(-105, SignalQualityRating.Poor)]
    [InlineData(-115, SignalQualityRating.Poor)]
    public void Classify_StandardLte_ReturnsExpectedRating(int dbm, SignalQualityRating expected)
    {
        var rating = SignalClassifier.Classify(dbm, "LTE");
        rating.Should().Be(expected);
    }

    [Theory]
    [InlineData(-75, SignalQualityRating.Excellent)]
    [InlineData(-80, SignalQualityRating.Excellent)]
    [InlineData(-88, SignalQualityRating.Good)]
    [InlineData(-95, SignalQualityRating.Good)]
    [InlineData(-102, SignalQualityRating.Fair)]
    [InlineData(-110, SignalQualityRating.Fair)]
    [InlineData(-118, SignalQualityRating.Poor)]
    public void Classify_5GNr_ReturnsExpectedRating(int dbm, SignalQualityRating expected)
    {
        var rating = SignalClassifier.Classify(dbm, "5G NR");
        rating.Should().Be(expected);
    }

    [Fact]
    public void Classify_NullSignal_ReturnsUnavailable()
    {
        var rating = SignalClassifier.Classify(null, "5G NR");
        rating.Should().Be(SignalQualityRating.Unavailable);
    }

    [Theory]
    [InlineData(-50, 100)]
    [InlineData(-120, 0)]
    [InlineData(-85, 50)]
    public void GetSignalPercentage_MapsBoundsCorrectly(int dbm, int expectedPct)
    {
        int pct = SignalClassifier.GetSignalPercentage(dbm);
        pct.Should().Be(expectedPct);
    }
}
