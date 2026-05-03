using System;
using System.Threading.Tasks;

namespace Networking.OAuth;

public interface ISsoRequests
{
	Task<Uri> GetAutoLoginUrlAsync(string url);
}
