using System.Threading.Tasks;
using Networking.Ghs.Dto;

namespace Networking.Ghs;

public interface IAccountService
{
	Task<AccountStatus> GetAccountStatusAsync();
}
