using CellScope.Application.DTOs;
using CellScope.Domain.Enums;

namespace CellScope.Mobile.Services;

public class PlatformCellMetric<T>
{
    public T? Value { get; set; }
    public DataAvailability Availability { get; set; } = DataAvailability.Unknown;
    public string? Reason { get; set; }

    public static PlatformCellMetric<T> Available(T value) => new() { Value = value, Availability = DataAvailability.Available };
    public static PlatformCellMetric<T> Unavailable(string reason = "Not exposed by device firmware/modem") => new() { Availability = DataAvailability.Unavailable, Reason = reason };
    public static PlatformCellMetric<T> Restricted(string reason = "Restricted by Android OS permissions") => new() { Availability = DataAvailability.Restricted, Reason = reason };
}

public interface ICellularInfoService
{
    Task<CellularSnapshotDto?> GetCurrentSnapshotAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NeighborCellDto>> GetNeighborCellsAsync(CancellationToken cancellationToken = default);
    Task<bool> HasTelephonyPermissionsAsync();
}
