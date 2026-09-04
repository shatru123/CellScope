using CellScope.Application.DTOs;

namespace CellScope.Mobile.Services;

public interface ILocationService
{
    Task<LocationPointDto?> GetCurrentLocationAsync(CancellationToken cancellationToken = default);
    Task<bool> HasLocationPermissionsAsync();
}
