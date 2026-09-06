using CellScope.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace CellScope.UnitTests;

public class PhoneNumberIntelligenceTests
{
    private readonly PhoneNumberIntelligenceService _service = new();
    private readonly PhoneNumberIntelligenceService _serviceWithDemo = new(new DemoDataService());

    [Fact]
    public async Task AnalyzePhoneNumberAsync_ValidIndiaJioNumber_ReturnsAccurateCircleAndCarrier()
    {
        // 98220xxxxx is traditionally Maharashtra circle
        var result = await _service.AnalyzePhoneNumberAsync("+91 98220 12345");

        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.CountryCode.Should().Be("IN");
        result.CountryName.Should().Be("India");
        result.TelecomCircle.Should().Contain("Maharashtra");
        result.OriginalCarrier.Should().NotBeNullOrWhiteSpace();
        result.LineType.Should().Contain("Mobile");
        result.RiskScore.Should().BeLessThan(35);
        result.ConsensualTrackingUrl.Should().Contain("consent=prompt");
        result.ConsensualTrackingUrl.Should().Contain("919822012345");
    }

    [Fact]
    public async Task AnalyzePhoneNumberAsync_IndiaNumberWithoutPlus_NormalizesCorrectly()
    {
        // 9604466334 without +91
        var result = await _service.AnalyzePhoneNumberAsync("9604466334");

        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.CountryName.Should().Be("India");
        result.CountryCode.Should().Be("IN");
        result.TelecomCircle.Should().Contain("Maharashtra");
    }

    [Fact]
    public async Task AnalyzePhoneNumberAsync_IndiaNumberWith91Prefix_NormalizesCorrectly()
    {
        // 919604466334 without +
        var result = await _service.AnalyzePhoneNumberAsync("919604466334");

        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.CountryName.Should().Be("India");
        result.TelecomCircle.Should().Contain("Maharashtra");
    }

    [Fact]
    public async Task AnalyzePhoneNumberAsync_IndiaNumberWithLeadingZero_NormalizesCorrectly()
    {
        // 09604466334
        var result = await _service.AnalyzePhoneNumberAsync("09604466334");

        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.CountryName.Should().Be("India");
        result.TelecomCircle.Should().Contain("Maharashtra");
    }

    [Fact]
    public async Task AnalyzePhoneNumberAsync_PartialSeriesPrefix_MatchesCircleAndOperator()
    {
        // 9820 is Mumbai series
        var result = await _service.AnalyzePhoneNumberAsync("9820");

        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.TelecomCircle.Should().Contain("Mumbai");
        result.OriginalCarrier.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AnalyzePhoneNumberAsync_AttachedActiveSubscriber_DetectsServingSector()
    {
        // +91 96044 66334 is an active subscriber UE in DemoDataService
        var result = await _serviceWithDemo.AnalyzePhoneNumberAsync("+91 96044 66334");

        result.Should().NotBeNull();
        result.IsAttachedToNetwork.Should().BeTrue();
        result.ServingTowerName.Should().NotBeNullOrWhiteSpace();
        result.ServingCellId.Should().NotBeNullOrWhiteSpace();
        result.ServingLatitude.Should().NotBeNull();
        result.ServingLongitude.Should().NotBeNull();
    }

    [Fact]
    public async Task AnalyzePhoneNumberAsync_ValidIndiaDelhiNumber_IdentifiesDelhiCircle()
    {
        // 98110xxxxx is Delhi Circle (Vodafone Idea / Airtel historical)
        var result = await _service.AnalyzePhoneNumberAsync("+91 98110 54321");

        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.CountryName.Should().Be("India");
        result.TelecomCircle.Should().Contain("Delhi");
        result.LineType.Should().Contain("Mobile");
    }

    [Fact]
    public async Task AnalyzePhoneNumberAsync_ValidUSNumber_IdentifiesNANPAreaCode()
    {
        // 415 is San Francisco, CA
        var result = await _service.AnalyzePhoneNumberAsync("+1 415 555 0199");

        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.CountryCode.Should().Be("US");
        result.CountryName.Should().Be("United States / Canada");
        result.TelecomCircle.Should().Contain("San Francisco");
        result.TelecomCircle.Should().Contain("California");
        result.LineType.Should().Contain("Mobile");
    }

    [Fact]
    public async Task AnalyzePhoneNumberAsync_ValidUKMobile_IdentifiesUnitedKingdom()
    {
        var result = await _service.AnalyzePhoneNumberAsync("+44 7911 123456");

        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.CountryCode.Should().Be("GB");
        result.CountryName.Should().Be("United Kingdom");
        result.LineType.Should().Contain("Mobile");
    }

    [Fact]
    public async Task AnalyzePhoneNumberAsync_RepeatedDigits_FlagsHighSpoofRisk()
    {
        // Number with all identical digits (e.g., 9999999999) is high spoof risk
        var result = await _service.AnalyzePhoneNumberAsync("+91 99999 99999");

        result.Should().NotBeNull();
        result.RiskScore.Should().BeGreaterThan(50);
        result.RiskFactors.Should().Contain(f => f.Contains("repetitive"));
    }

    [Fact]
    public async Task AnalyzePhoneNumberAsync_EmptyOrInvalid_ReturnsInvalidResult()
    {
        var result = await _service.AnalyzePhoneNumberAsync("not-a-number");

        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.LineType.Should().Be("Unknown / Invalid");
    }
}
