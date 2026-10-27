using System.ComponentModel.DataAnnotations;

namespace TMS2000.Shared.DTO.Auth;

public record RefreshTokenRequest(

    [Required] string RefreshToken);