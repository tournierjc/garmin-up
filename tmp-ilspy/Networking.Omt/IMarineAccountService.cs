using System.Threading.Tasks;
using Networking.Omt.Dto.MarineAccountService;

namespace Networking.Omt;

public interface IMarineAccountService
{
	Task<RegisteredDeviceCategory[]> GetRegisteredDevicesForCustomerAsync(string locale);

	Task<CardRegistration[]> RegisterCardsToCustomerAsync(string[] gmas);

	Task<DeviceRegistration[]> RegisterDevicesToCustomerAsync(long[] unitIds);
}
