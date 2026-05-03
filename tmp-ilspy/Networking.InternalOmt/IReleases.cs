using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.InternalOmt.Dto;

namespace Networking.InternalOmt;

public interface IReleases
{
	Task<Release> GetReleaseByIdentifierAsync(Identifier releaseIdentifier);

	Task<List<Release>> GetReleasesByProductGroupAsync(string productGroupCode, bool activeOnly = false);
}
