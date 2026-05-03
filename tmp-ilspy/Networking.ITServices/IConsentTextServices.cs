using System.Threading.Tasks;
using Networking.ITServices.Dto;

namespace Networking.ITServices;

public interface IConsentTextServices
{
	Task<ConsentContent[]> GetContentAsync(string consentType, string locale);
}
