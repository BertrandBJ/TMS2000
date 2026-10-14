using System.ComponentModel.DataAnnotations;

namespace TMS2000.Shared.DTO.Auth;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
/*
public class LoginRequest(string Email, string Password)
{
    public string Email { get; init; } = Email;
    public string Password { get; init; } = Password;

    public void Deconstruct(out string Email, out string Password)
    {
        Email = this.Email;
        Password = this.Password;
    }
}

Record produit la même chose que ce qui est au dessus, c'est une écriture plus dense pour créer une classe rapidement (dans le cas des dtos par exemple c'est pratique*/