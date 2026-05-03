using System.Threading.Tasks;

namespace Networking.Omt;

public interface INetworkSpeedTest
{
	Task<double> RunDownloadSpeedTestAsync();
}
