using System.Threading.Tasks;

namespace Networking.Connect;

public interface IUserService
{
	Task SendTimezoneToConnectAsync(string timeZone);
}
