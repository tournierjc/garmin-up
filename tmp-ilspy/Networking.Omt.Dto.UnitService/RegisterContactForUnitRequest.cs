using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.UnitService;

public class RegisterContactForUnitRequest
{
	public object ClientInfo => new object();

	public BasicUnitInfo BasicUnitInfo { get; }

	public string EmailAddress { get; }

	public bool DidOptIn { get; }

	public string? ConsentTypeId { get; }

	public string? ConsentTypeVersion { get; }

	public RegisterContactForUnitRequest(BasicUnitInfo basicUnitInfo, string emailAddress, bool didOptIn, string? consentTypeId, string? consentTypeVersion)
	{
		BasicUnitInfo = basicUnitInfo;
		EmailAddress = emailAddress;
		DidOptIn = didOptIn;
		ConsentTypeId = consentTypeId;
		ConsentTypeVersion = consentTypeVersion;
	}
}
