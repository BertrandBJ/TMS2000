namespace TMS2000.Shared.DTO.Auth;


public record LoginResponse(
    string AccessToken,
    DateTime AccessTokenExpirationDate,
    string RefreshToken,
    DateTime RefreshTokenExpirationDate,
    string Role
    );