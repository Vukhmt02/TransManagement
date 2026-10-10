using System.ComponentModel.DataAnnotations;

namespace TransManagement.API.Contracts.Auth;

public sealed record RequestRegistrationOtpRequest(
    [Required, EmailAddress] string Email);
