namespace Networking.Omt.Dto.MarineAccountService;

public record DeviceRegistration(long UnitId, RegistrationStatus StatusCode = RegistrationStatus.Unknown);
