namespace TMS2000.Shared.DTO.Auth;

public record RegisterRequest(string Email, string Password, string Role);