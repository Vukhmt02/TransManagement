using System.ComponentModel.DataAnnotations;

namespace TransManagement.API.Contracts.Auth;

public sealed record VerifyRegistrationOtpRequest(
    [Required, EmailAddress] string Email,
    [Required, RegularExpression("^[0-9]{6}$")] string Otp,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(200)] string FullName,
    [Required, MaxLength(30)] string Phone);
