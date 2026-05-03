using System.Threading.Tasks;
using DeviceXmlUtil;
using Networking.Omt.Dto.UnitService;

namespace Networking.Omt;

public interface IUnitService
{
	Task<UnitDisplayInfo> GetUnitDisplayInfoAsync(string? firmwarePartNumber);

	Task RegisterContactForUnitAsync(RegisterContactForUnitRequest request);

	Task AssociateVehicleAsync(XmlDevice xmlDevice);

	Task<string> GetMapCareSubscriptionAsync();

	Task<UnitManual[]> GetUnitManualsAsync();
}
