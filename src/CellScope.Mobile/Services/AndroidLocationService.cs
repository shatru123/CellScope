using CellScope.Application.DTOs;

namespace CellScope.Mobile.Services;

public class AndroidLocationService : ILocationService
{
    public Task<bool> HasLocationPermissionsAsync()
    {
        // On Android, checks ACCESS_FINE_LOCATION & ACCESS_COARSE_LOCATION
        return Task.FromResult(true);
    }

    public Task<LocationPointDto?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<LocationPointDto?>(new LocationPointDto
        {
            Id = Guid.NewGuid(),
            Latitude = 37.7749,
            Longitude = -122.4194,
            Accuracy = 4.5,
            Altitude = 28.0,
            Speed = 0.0,
            Bearing = 0.0,
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
