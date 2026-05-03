using System.Threading.Tasks;
using RestSharp;

namespace Networking.Connect;

public interface IConsentService
{
	Task<RestResponse> GetConsentAsync(string consentType);

	Task<RestResponse> SetConsentAsync(bool consented, string consentType, string locale, string version);

	Task<RestResponse> RevokeConsentAsync(string consentType, bool clientInitiated, string locale, string version, long userId, string passwordChallengeToken);
}
