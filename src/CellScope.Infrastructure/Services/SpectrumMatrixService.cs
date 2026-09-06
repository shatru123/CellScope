using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;

namespace CellScope.Infrastructure.Services;

public class SpectrumMatrixService : ISpectrumMatrixService
{
    private static readonly List<SpectrumAllocationDto> _allocations = new()
    {
        new()
        {
            BandNumber = "n78",
            BandName = "3.5 GHz Mid-Band C-Band",
            FrequencyRange = "3300 - 3800 MHz",
            DuplexMode = "TDD (Time Division Duplex)",
            TypicalBandwidthsMhz = "100 MHz / 50 MHz",
            Generation = "5G NR",
            PrimaryUse = "Primary High-Capacity 5G Gigabit Data Layer (eMBB)",
            KeyOperators = new() { "Reliance Jio", "Bharti Airtel", "Vodafone Idea" },
            TechnicalDescription = "The global harmonized golden band for 5G NR. Delivers multi-gigabit throughput via Massive MIMO beamforming with 64T64R antenna arrays.",
            CircleHoldings = new()
            {
                ["Maharashtra & Goa"] = new() { "Reliance Jio (100 MHz)", "Bharti Airtel (100 MHz)", "Vodafone Idea (50 MHz)" },
                ["Mumbai"] = new() { "Reliance Jio (100 MHz)", "Bharti Airtel (100 MHz)", "Vodafone Idea (50 MHz)" },
                ["Delhi NCR"] = new() { "Reliance Jio (100 MHz)", "Bharti Airtel (100 MHz)", "Vodafone Idea (50 MHz)" },
                ["Karnataka"] = new() { "Reliance Jio (100 MHz)", "Bharti Airtel (100 MHz)", "Vodafone Idea (50 MHz)" },
                ["Tamil Nadu / Chennai"] = new() { "Reliance Jio (100 MHz)", "Bharti Airtel (100 MHz)" },
                ["Andhra Pradesh / Telangana"] = new() { "Reliance Jio (100 MHz)", "Bharti Airtel (100 MHz)" },
                ["Gujarat"] = new() { "Reliance Jio (100 MHz)", "Bharti Airtel (100 MHz)", "Vodafone Idea (50 MHz)" },
                ["Kolkata & West Bengal"] = new() { "Reliance Jio (100 MHz)", "Bharti Airtel (100 MHz)" },
                ["Punjab & Haryana"] = new() { "Reliance Jio (100 MHz)", "Bharti Airtel (100 MHz)" }
            }
        },
        new()
        {
            BandNumber = "n258",
            BandName = "26 GHz High-Band mmWave",
            FrequencyRange = "24.25 - 27.5 GHz",
            DuplexMode = "TDD (Time Division Duplex)",
            TypicalBandwidthsMhz = "800 MHz / 1000 MHz",
            Generation = "5G NR",
            PrimaryUse = "Ultra-High Density mmWave Hotspots & Enterprise Private 5G",
            KeyOperators = new() { "Reliance Jio", "Bharti Airtel", "Adani Data Networks" },
            TechnicalDescription = "Extreme capacity millimeter-wave layer capable of 3+ Gbps speeds in dense stadiums, airport hubs, campus networks, and industrial robotics.",
            CircleHoldings = new()
            {
                ["Maharashtra & Goa"] = new() { "Reliance Jio (1000 MHz)", "Bharti Airtel (800 MHz)" },
                ["Mumbai"] = new() { "Reliance Jio (1000 MHz)", "Bharti Airtel (800 MHz)", "Adani (400 MHz)" },
                ["Delhi NCR"] = new() { "Reliance Jio (1000 MHz)", "Bharti Airtel (800 MHz)", "Adani (400 MHz)" },
                ["Karnataka"] = new() { "Reliance Jio (1000 MHz)", "Bharti Airtel (800 MHz)" },
                ["Gujarat"] = new() { "Reliance Jio (1000 MHz)", "Bharti Airtel (800 MHz)", "Adani (400 MHz)" }
            }
        },
        new()
        {
            BandNumber = "n28",
            BandName = "700 MHz Low-Band Digital Dividend",
            FrequencyRange = "703 - 748 MHz (UL) / 758 - 803 MHz (DL)",
            DuplexMode = "FDD (Frequency Division Duplex)",
            TypicalBandwidthsMhz = "10 MHz paired",
            Generation = "5G NR",
            PrimaryUse = "Wide-Area Macro 5G Standalone (SA) Coverage & Deep Indoor Penetration",
            KeyOperators = new() { "Reliance Jio" },
            TechnicalDescription = "Sub-1GHz golden coverage band. Provides deep building penetration through concrete structures and vast rural coverage reach without dead zones.",
            CircleHoldings = new()
            {
                ["Maharashtra & Goa"] = new() { "Reliance Jio (10 MHz FDD)" },
                ["Mumbai"] = new() { "Reliance Jio (10 MHz FDD)" },
                ["Delhi NCR"] = new() { "Reliance Jio (10 MHz FDD)" },
                ["Karnataka"] = new() { "Reliance Jio (10 MHz FDD)" },
                ["Pan-India (All 22 Circles)"] = new() { "Reliance Jio (10 MHz Nationwide)" }
            }
        },
        new()
        {
            BandNumber = "Band 40",
            BandName = "2300 MHz TDD Broadband",
            FrequencyRange = "2300 - 2400 MHz",
            DuplexMode = "TDD (Time Division Duplex)",
            TypicalBandwidthsMhz = "20 MHz / 40 MHz",
            Generation = "4G LTE",
            PrimaryUse = "High-Capacity Urban LTE Data Bearer",
            KeyOperators = new() { "Bharti Airtel", "Reliance Jio" },
            TechnicalDescription = "Workhorse 4G capacity band deployed on almost every urban macro cell across India and Asia. Aggregated with Band 3 for Carrier Aggregation (LTE-A).",
            CircleHoldings = new()
            {
                ["Maharashtra & Goa"] = new() { "Bharti Airtel (40 MHz)", "Reliance Jio (20 MHz)" },
                ["Mumbai"] = new() { "Bharti Airtel (40 MHz)", "Reliance Jio (20 MHz)" },
                ["Delhi NCR"] = new() { "Bharti Airtel (40 MHz)", "Reliance Jio (20 MHz)" },
                ["Karnataka"] = new() { "Bharti Airtel (40 MHz)", "Reliance Jio (20 MHz)" },
                ["Pan-India"] = new() { "Bharti Airtel (20-40 MHz)", "Reliance Jio (20 MHz)" }
            }
        },
        new()
        {
            BandNumber = "Band 3",
            BandName = "1800 MHz DCS Core Cellular",
            FrequencyRange = "1710 - 1785 MHz (UL) / 1805 - 1880 MHz (DL)",
            DuplexMode = "FDD (Frequency Division Duplex)",
            TypicalBandwidthsMhz = "10 MHz / 15 MHz / 20 MHz",
            Generation = "4G LTE / 2G",
            PrimaryUse = "Primary Ubiquitous 4G LTE Layer & VoLTE Voice",
            KeyOperators = new() { "Bharti Airtel", "Reliance Jio", "Vodafone Idea", "BSNL" },
            TechnicalDescription = "The primary balanced coverage and throughput band for 4G LTE worldwide. Forms the anchor carrier for 5G NSA (Non-Standalone) dual connectivity.",
            CircleHoldings = new()
            {
                ["Maharashtra & Goa"] = new() { "Bharti Airtel (15 MHz)", "Reliance Jio (10 MHz)", "Vodafone Idea (10 MHz)" },
                ["Mumbai"] = new() { "Bharti Airtel (15 MHz)", "Reliance Jio (10 MHz)", "Vodafone Idea (10.6 MHz)" },
                ["Delhi NCR"] = new() { "Bharti Airtel (15 MHz)", "Reliance Jio (10 MHz)", "Vodafone Idea (10 MHz)" },
                ["Karnataka"] = new() { "Bharti Airtel (15 MHz)", "Reliance Jio (10 MHz)", "Vodafone Idea (10 MHz)" },
                ["Pan-India"] = new() { "All Four Major Carriers" }
            }
        },
        new()
        {
            BandNumber = "Band 8",
            BandName = "900 MHz Extended Coverage",
            FrequencyRange = "880 - 915 MHz (UL) / 925 - 960 MHz (DL)",
            DuplexMode = "FDD (Frequency Division Duplex)",
            TypicalBandwidthsMhz = "5 MHz / 10 MHz",
            Generation = "4G LTE / GSM",
            PrimaryUse = "Sub-1GHz Macro Coverage, Indoor Fallback & IoT",
            KeyOperators = new() { "Bharti Airtel", "Vodafone Idea" },
            TechnicalDescription = "Low-frequency propagation layer refarmed from 2G to LTE. Reaches deep into underground basements, elevators, and dense building cores.",
            CircleHoldings = new()
            {
                ["Maharashtra & Goa"] = new() { "Bharti Airtel (7.4 MHz)", "Vodafone Idea (10 MHz)" },
                ["Mumbai"] = new() { "Bharti Airtel (6.2 MHz)", "Vodafone Idea (11 MHz)" },
                ["Delhi NCR"] = new() { "Bharti Airtel (10 MHz)", "Vodafone Idea (10 MHz)" },
                ["Karnataka"] = new() { "Bharti Airtel (7.4 MHz)", "Vodafone Idea (5 MHz)" }
            }
        },
        new()
        {
            BandNumber = "Band 41",
            BandName = "2500 MHz BRS/EBS TDD",
            FrequencyRange = "2496 - 2690 MHz",
            DuplexMode = "TDD (Time Division Duplex)",
            TypicalBandwidthsMhz = "20 MHz",
            Generation = "4G LTE",
            PrimaryUse = "Vodafone Idea (Vi) Turbo GIGAnet High Speed Layer",
            KeyOperators = new() { "Vodafone Idea" },
            TechnicalDescription = "Wideband TDD spectrum utilized by Vodafone Idea across 16 circles to power high-concurrency 4G data streaming.",
            CircleHoldings = new()
            {
                ["Maharashtra & Goa"] = new() { "Vodafone Idea (20 MHz)" },
                ["Mumbai"] = new() { "Vodafone Idea (20 MHz)" },
                ["Delhi NCR"] = new() { "Vodafone Idea (20 MHz)" },
                ["Gujarat"] = new() { "Vodafone Idea (20 MHz)" }
            }
        }
    };

    public IReadOnlyList<SpectrumAllocationDto> GetSpectrumAllocations(string? circleName = null, string? generation = null)
    {
        var query = _allocations.AsEnumerable();

        if (!string.IsNullOrEmpty(generation))
        {
            query = query.Where(a => a.Generation.Contains(generation, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(circleName) && circleName != "All Circles")
        {
            query = query.Where(a => a.CircleHoldings.Keys.Any(k => k.Contains(circleName, StringComparison.OrdinalIgnoreCase)) || a.CircleHoldings.ContainsKey("Pan-India"));
        }

        return query.ToList();
    }

    public IReadOnlyList<string> GetAvailableCircles()
    {
        return new List<string>
        {
            "All Circles",
            "Maharashtra & Goa",
            "Mumbai",
            "Delhi NCR",
            "Karnataka",
            "Tamil Nadu / Chennai",
            "Andhra Pradesh / Telangana",
            "Gujarat",
            "Kolkata & West Bengal",
            "Punjab & Haryana"
        };
    }
}
