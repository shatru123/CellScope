using CellScope.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace CellScope.UnitTests;

public class PhoneNumberIntelligenceTests
{
    private readonly PhoneNumberIntelligenceService _service = new();

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
