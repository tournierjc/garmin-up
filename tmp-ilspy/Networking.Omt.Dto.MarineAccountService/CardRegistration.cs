namespace Networking.Omt.Dto.MarineAccountService;

public record CardRegistration(string Gma, RegistrationStatus StatusCode = RegistrationStatus.Unknown);
