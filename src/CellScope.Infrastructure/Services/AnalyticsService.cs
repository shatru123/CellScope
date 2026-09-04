using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using CellScope.Domain.Services;
using CellScope.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CellScope.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly CellScopeDbContext _dbContext;

    public AnalyticsService(CellScopeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SignalAnalyticsDto> GetAnalyticsAsync(
        string timeRange = "24h", string? operatorName = null, string? technology = null, CancellationToken cancellationToken = default)
    {
        var cutoff = timeRange.ToLowerInvariant() switch
        {
            "1h" => DateTimeOffset.UtcNow.AddHours(-1),
            "7d" => DateTimeOffset.UtcNow.AddDays(-7),
            "30d" => DateTimeOffset.UtcNow.AddDays(-30),
            _ => DateTimeOffset.UtcNow.AddHours(-24)
        };

        var observations = new List<Domain.Entities.SignalObservation>();
        int handoversCount = 0;

        try
        {
            var query = _dbContext.SignalObservations.AsNoTracking().Where(s => s.Timestamp >= cutoff);

            if (!string.IsNullOrEmpty(operatorName))
                query = query.Where(s => s.OperatorName == operatorName);

            if (!string.IsNullOrEmpty(technology))
                query = query.Where(s => s.RadioTechnology == technology);

            observations = await query.OrderBy(s => s.Timestamp).ToListAsync(cancellationToken);

            handoversCount = await _dbContext.CellHandovers
                .AsNoTracking()
                .CountAsync(h => h.Timestamp >= cutoff, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AnalyticsService Notice] GetAnalyticsAsync fallback: {ex.Message}");
        }

        var result = new SignalAnalyticsDto
        {
            TotalObservations = observations.Count,
            TotalHandovers = handoversCount
        };

        if (observations.Count == 0)
        {
            return result;
        }

        result.AverageSignalStrength = Math.Round(observations.Average(o => o.SignalStrengthDbm), 1);
        result.MinSignalStrength = observations.Min(o => o.SignalStrengthDbm);
        result.MaxSignalStrength = observations.Max(o => o.SignalStrengthDbm);

        // Signal strength time series (sample down if > 60 points)
        int step = Math.Max(1, observations.Count / 60);
        for (int i = 0; i < observations.Count; i += step)
        {
            var obs = observations[i];
            result.SignalStrengthTrend.Add(new TimeSeriesPoint<int>
            {
                Timestamp = obs.Timestamp,
                Value = obs.SignalStrengthDbm,
                Label = obs.Timestamp.ToString("HH:mm:ss")
            });

            if (obs.SignalQuality.HasValue)
            {
                result.SignalQualityTrend.Add(new TimeSeriesPoint<double>
                {
                    Timestamp = obs.Timestamp,
                    Value = obs.SignalQuality.Value,
                    Label = obs.Timestamp.ToString("HH:mm:ss")
                });
            }
        }

        // Tech distribution
        result.TechnologyDistribution = observations
            .GroupBy(o => o.RadioTechnology ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        // Operator average signal
        result.OperatorAverageSignal = observations
            .Where(o => !string.IsNullOrEmpty(o.OperatorName))
            .GroupBy(o => o.OperatorName!)
            .ToDictionary(g => g.Key, g => Math.Round(g.Average(x => x.SignalStrengthDbm), 1));

        // Rating breakdown
        result.RatingDistribution = observations
            .GroupBy(o => SignalClassifier.GetRatingText(SignalClassifier.Classify(o.SignalStrengthDbm, o.RadioTechnology)))
            .ToDictionary(g => g.Key, g => g.Count());

        return result;
    }
}
