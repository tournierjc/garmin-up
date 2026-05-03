using System.Threading.Tasks;
using DeviceXmlUtil;
using Networking.Omt.Dto.DeviceSoftware;

namespace Networking.Omt;

public interface IDeviceSoftware
{
	Task<DeviceSoftwareUpdateResponse> GetMarineSoftwareUpdateAsync();

	Task<DeviceSoftwareUpdateResponse> GetMarineSoftwareUpdateAsync(XmlDevice xmlDevice);
}
