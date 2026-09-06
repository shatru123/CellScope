using CellScope.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace CellScope.UnitTests;

public class SpectrumMatrixTests
{
    private readonly SpectrumMatrixService _service = new();

    [Fact]
    public void GetSpectrumAllocations_ReturnsStandard5GAnd4GBands()
    {
        var bands = _service.GetSpectrumAllocations();

        bands.Should().NotBeNull();
        bands.Should().NotBeEmpty();

        // 3GPP Band n78 (3500 MHz C-Band) must be present
        bands.Should().Contain(b => b.BandNumber == "n78" && b.DuplexMode.Contains("TDD"));

        // 3GPP Band 3 (1800 MHz) must be present
        bands.Should().Contain(b => b.BandNumber == "Band 3" && b.DuplexMode.Contains("FDD"));
    }

    [Fact]
    public void GetSpectrumAllocations_FilterByGeneration_ReturnsOnlySpecifiedGeneration()
    {
        var fiveGBands = _service.GetSpectrumAllocations(generation: "5G NR");
        fiveGBands.Should().NotBeEmpty();
        fiveGBands.Should().OnlyContain(b => b.Generation.Contains("5G NR"));

        var fourGBands = _service.GetSpectrumAllocations(generation: "4G LTE");
        fourGBands.Should().NotBeEmpty();
        fourGBands.Should().OnlyContain(b => b.Generation.Contains("4G LTE"));
    }

    [Fact]
    public void GetSpectrumAllocations_FilterByCircle_ReturnsCircleHoldings()
    {
        var holdings = _service.GetSpectrumAllocations(circleName: "Maharashtra & Goa");

        holdings.Should().NotBeNull();
        holdings.Should().NotBeEmpty();

        // Should include Jio and Airtel 5G holdings
        holdings.Should().Contain(h => h.KeyOperators.Contains("Reliance Jio"));
        holdings.Should().Contain(h => h.KeyOperators.Contains("Bharti Airtel"));
    }

    [Fact]
    public void GetAvailableCircles_ReturnsExpectedCircles()
    {
        var circles = _service.GetAvailableCircles();

        circles.Should().NotBeNull();
        circles.Should().Contain("Maharashtra & Goa");
        circles.Should().Contain("Mumbai");
        circles.Should().Contain("Delhi NCR");
    }
}
