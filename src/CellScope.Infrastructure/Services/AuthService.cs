using System.Security.Cryptography;
using System.Text;
using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using CellScope.Domain.Entities;
using CellScope.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CellScope.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly CellScopeDbContext _dbContext;

    public AuthService(CellScopeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        string username = request.Username.Trim();
        string email = request.Email.Trim().ToLowerInvariant();

        if (await _dbContext.Users.AnyAsync(u => u.Username == username || u.Email == email, cancellationToken))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Username or email is already registered."
            };
        }

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = HashPassword(request.Password),
            Role = "User",
            CreatedAt = DateTimeOffset.UtcNow,
            Settings = new UserSettings()
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            Success = true,
            Message = "User registered successfully.",
            Token = GenerateToken(user),
            RefreshToken = Guid.NewGuid().ToString("N"),
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            }
        };
    }

    public async Task<AuthResponse> LoginAsync(AuthRequest request, CancellationToken cancellationToken = default)
    {
        string identifier = request.UsernameOrEmail.Trim();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == identifier || u.Email == identifier.ToLowerInvariant(), cancellationToken);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid credentials."
            };
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            Success = true,
            Message = "Login successful.",
            Token = GenerateToken(user),
            RefreshToken = Guid.NewGuid().ToString("N"),
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            }
        };
    }

    public async Task<UserSettingsDto> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (settings == null)
        {
            settings = new UserSettings { UserId = userId };
            _dbContext.UserSettings.Add(settings);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new UserSettingsDto
        {
            LocationCollectionEnabled = settings.LocationCollectionEnabled,
            CellularCollectionEnabled = settings.CellularCollectionEnabled,
            LocalNetworkDiscoveryEnabled = settings.LocalNetworkDiscoveryEnabled,
            CloudSyncEnabled = settings.CloudSyncEnabled,
            DataRetentionDays = settings.DataRetentionDays,
            Theme = settings.Theme,
            CollectionIntervalSeconds = settings.CollectionIntervalSeconds,
            BatterySavingMode = settings.BatterySavingMode
        };
    }

    public async Task<UserSettingsDto> UpdateSettingsAsync(Guid userId, UserSettingsDto dto, CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (settings == null)
        {
            settings = new UserSettings { UserId = userId };
            _dbContext.UserSettings.Add(settings);
        }

        settings.LocationCollectionEnabled = dto.LocationCollectionEnabled;
        settings.CellularCollectionEnabled = dto.CellularCollectionEnabled;
        settings.LocalNetworkDiscoveryEnabled = dto.LocalNetworkDiscoveryEnabled;
        settings.CloudSyncEnabled = dto.CloudSyncEnabled;
        settings.DataRetentionDays = dto.DataRetentionDays;
        settings.Theme = dto.Theme;
        settings.CollectionIntervalSeconds = dto.CollectionIntervalSeconds;
        settings.BatterySavingMode = dto.BatterySavingMode;
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return dto;
    }

    public async Task<bool> PurgeTelemetryDataAsync(Guid userId, string target = "all", CancellationToken cancellationToken = default)
    {
        if (target == "all" || target == "cellular")
        {
            var snapshots = await _dbContext.CellularSnapshots.ToListAsync(cancellationToken);
            _dbContext.CellularSnapshots.RemoveRange(snapshots);

            var signalObs = await _dbContext.SignalObservations.ToListAsync(cancellationToken);
            _dbContext.SignalObservations.RemoveRange(signalObs);

            var handovers = await _dbContext.CellHandovers.ToListAsync(cancellationToken);
            _dbContext.CellHandovers.RemoveRange(handovers);
        }

        if (target == "all" || target == "locations")
        {
            var locations = await _dbContext.LocationPoints.ToListAsync(cancellationToken);
            _dbContext.LocationPoints.RemoveRange(locations);
        }

        if (target == "all" || target == "devices")
        {
            var netDevices = await _dbContext.NetworkDevices.ToListAsync(cancellationToken);
            _dbContext.NetworkDevices.RemoveRange(netDevices);

            var localNets = await _dbContext.LocalNetworks.ToListAsync(cancellationToken);
            _dbContext.LocalNetworks.RemoveRange(localNets);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;

        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] expectedHash = Convert.FromBase64String(parts[1]);

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static string GenerateToken(User user)
    {
        return $"cellscope-jwt-{user.Id}-{Guid.NewGuid():N}";
    }
}
