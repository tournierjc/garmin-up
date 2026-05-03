using System.Threading.Tasks;
using RestSharp;

namespace Networking.Omt;

public interface IUploadConsent
{
	Task<RestResponse> GetUnitConsents(string clientId);

	Task<RestResponse> SetUnitConsent(string consentType, string consentState, string locale, string version, string clientId);
}
