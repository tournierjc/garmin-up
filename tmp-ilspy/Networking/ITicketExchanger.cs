using System.Threading.Tasks;
using Networking.Auth;
using Networking.OAuth;

namespace Networking;

public interface ITicketExchanger
{
	Task<ConnectAuth> GetConnectAuthAsync(ServiceTicket ticket);

	Task<DIToken> GetDIAuthAsync(ServiceTicket ticket);

	Task<ITAuth> GetITAuthAsync(ServiceTicket ticket);
}
