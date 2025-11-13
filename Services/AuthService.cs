using AutoMapper;
using BCrypt.Net;
using E_commerce_Application.Configuration;
using E_commerce_Application.DbContext;
using E_commerce_Application.DTOs.Auth;
using E_commerce_Application.Entities;
using E_commerce_Application.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace E_commerce_Application.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        ApplicationDbContext context,
        ITokenService tokenService,
        IMapper mapper,
        IOptions<JwtSettings> jwtOptions)
    {
        _context = context;
        _tokenService = tokenService;
        _mapper = mapper;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (existingUser)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Customer
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _tokenService.RevokeRefreshTokensAsync(user.Id);

        var (accessToken, accessExpires) = await _tokenService.GenerateAccessTokenAsync(user);
        var (refreshToken, refreshExpires) = await _tokenService.GenerateRefreshTokenAsync(user);

        await _tokenService.SaveRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            Expires = refreshExpires
        });

        return BuildAuthResponse(user, accessToken, accessExpires, refreshToken, refreshExpires);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        await _tokenService.RevokeRefreshTokensAsync(user.Id);

        var (accessToken, accessExpires) = await _tokenService.GenerateAccessTokenAsync(user);
        var (refreshToken, refreshExpires) = await _tokenService.GenerateRefreshTokenAsync(user);

        await _tokenService.SaveRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            Expires = refreshExpires
        });

        return BuildAuthResponse(user, accessToken, accessExpires, refreshToken, refreshExpires);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var storedToken = await _tokenService.GetRefreshTokenAsync(request.RefreshToken);
        if (storedToken is null || storedToken.IsExpired || storedToken.IsRevoked || storedToken.User is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = storedToken.User;

        storedToken.IsRevoked = true;
        await _context.SaveChangesAsync();

        var (accessToken, accessExpires) = await _tokenService.GenerateAccessTokenAsync(user);
        var (refreshToken, refreshExpires) = await _tokenService.GenerateRefreshTokenAsync(user);

        await _tokenService.SaveRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            Expires = refreshExpires
        });

        return BuildAuthResponse(user, accessToken, accessExpires, refreshToken, refreshExpires);
    }

    private static AuthResponse BuildAuthResponse(User user, string accessToken, DateTime accessExpires, string refreshToken, DateTime refreshExpires)
    {
        return new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = accessExpires,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpires,
            Role = user.Role.ToString(),
            Name = user.Name,
            Email = user.Email
        };
    }
}

