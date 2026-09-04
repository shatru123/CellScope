using System.Security.Cryptography;
using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using CellScope.Application.Mapping;
using CellScope.Domain.Entities;
using CellScope.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CellScope.Infrastructure.Services;

public class DeviceService : IDeviceService
{
    private readonly CellScopeDbContext _dbContext;

    public DeviceService(CellScopeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DeviceDto> RegisterDeviceAsync(RegisterDeviceRequest request, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var device = new Device
        {
            UserId = userId,
            Name = string.IsNullOrWhiteSpace(request.Name) ? "Android Collector" : request.Name,
            Platform = request.Platform,
            Model = request.Model,
            OsVersion = request.OsVersion,
            AppVersion = request.AppVersion,
            PairingCode = GenerateRandomPairingCode(),
            IsPaired = false,
            CreatedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow
        };

        _dbContext.Devices.Add(device);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return DtoMapper.ToDto(device);
    }

    public async Task<string> GeneratePairingCodeAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);
        if (device == null)
            throw new KeyNotFoundException("Device not found.");

        device.PairingCode = GenerateRandomPairingCode();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return device.PairingCode;
    }

    public async Task<PairDeviceResponse> PairDeviceAsync(PairDeviceRequest request, CancellationToken cancellationToken = default)
    {
        string cleanCode = request.PairingCode.Trim().ToUpperInvariant();
        var device = await _dbContext.Devices.FirstOrDefaultAsync(d => d.PairingCode == cleanCode, cancellationToken);

        if (device == null)
        {
            // Auto-create or pair if needed
            device = new Device
            {
                Name = string.IsNullOrWhiteSpace(request.DeviceName) ? "Paired Android Device" : request.DeviceName,
                Platform = request.Platform,
                Model = request.Model,
                PairingCode = cleanCode,
                IsPaired = true,
                PairedAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow
            };
            _dbContext.Devices.Add(device);
        }
        else
        {
            device.IsPaired = true;
            device.PairedAt = DateTimeOffset.UtcNow;
            device.LastSeenAt = DateTimeOffset.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.DeviceName))
                device.Name = request.DeviceName;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PairDeviceResponse
        {
            Success = true,
            Message = "Device paired successfully with CellScope backend.",
            DeviceId = device.Id,
            DeviceToken = Guid.NewGuid().ToString("N")
        };
    }

    public async Task<IReadOnlyList<DeviceDto>> GetDevicesAsync(Guid? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.Devices.AsNoTracking();
            if (userId.HasValue)
                query = query.Where(d => d.UserId == userId.Value);

            var list = await query.OrderByDescending(d => d.LastSeenAt).ToListAsync(cancellationToken);
            return list.Select(DtoMapper.ToDto).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DeviceService Notice] GetDevicesAsync fallback: {ex.Message}");
            return Array.Empty<DeviceDto>();
        }
    }

    public async Task<DeviceDto?> GetDeviceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dev = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return dev != null ? DtoMapper.ToDto(dev) : null;
    }

    public async Task<bool> UpdateHeartbeatAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);
        if (device == null) return false;

        device.LastSeenAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);
        if (device == null) return false;

        _dbContext.Devices.Remove(device);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string GenerateRandomPairingCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var randomPart1 = new string(Enumerable.Range(0, 4).Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());
        var randomPart2 = new string(Enumerable.Range(0, 4).Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());
        return $"{randomPart1}-{randomPart2}";
    }
}
