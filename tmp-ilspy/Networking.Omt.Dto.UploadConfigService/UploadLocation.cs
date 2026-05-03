using System;

namespace Networking.Omt.Dto.UploadConfigService;

public class UploadLocation
{
	public required bool RequiresAuthentication { get; init; }

	public required bool RequiresUserConsent { get; init; }

	public required Uri Url { get; init; }

	public required ConsentType[] RequiredConsentTypes { get; init; }

	public required int AuthenticationType { get; init; }
}
