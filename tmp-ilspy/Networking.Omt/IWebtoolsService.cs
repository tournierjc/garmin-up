using System.Threading.Tasks;

namespace Networking.Omt;

public interface IWebtoolsService
{
	Task<bool> GetIsIntranetAsync();
}
