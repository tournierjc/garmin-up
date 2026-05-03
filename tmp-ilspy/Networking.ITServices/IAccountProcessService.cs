using System.Threading.Tasks;

namespace Networking.ITServices;

public interface IAccountProcessService
{
	Task<string> CheckPasswordAsync(string password);
}
