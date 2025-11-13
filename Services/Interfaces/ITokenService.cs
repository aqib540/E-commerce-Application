using E_commerce_Application.Entities;

namespace E_commerce_Application.Services.Interfaces;

public interface ITokenService
{
    Task<(string AccessToken, DateTime ExpiresAt)> GenerateAccessTokenAsync(User user);
    Task<(string RefreshToken, DateTime ExpiresAt)> GenerateRefreshTokenAsync(User user);
    Task RevokeRefreshTokensAsync(Guid userId);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task SaveRefreshTokenAsync(RefreshToken refreshToken);
}

