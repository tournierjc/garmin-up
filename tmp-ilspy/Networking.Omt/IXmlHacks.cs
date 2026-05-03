using System.Threading.Tasks;
using DeviceXmlUtil;

namespace Networking.Omt;

public interface IXmlHacks
{
	Task<XmlDevice> GetRepairedDeviceXmlAsync(XmlDevice xmlDevice);
}
