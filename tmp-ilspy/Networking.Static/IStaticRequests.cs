using System.Collections.Generic;
using System.Threading.Tasks;

namespace Networking.Static;

public interface IStaticRequests
{
	Task<Dictionary<string, int>> GetSsoMinimumAges();
}
