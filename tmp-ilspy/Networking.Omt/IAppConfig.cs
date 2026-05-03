using System.Threading.Tasks;
using Networking.Omt.Dto.AppConfig;

namespace Networking.Omt;

public interface IAppConfig
{
	Task<AppRule[]> GetAppConfigAsync();
}
