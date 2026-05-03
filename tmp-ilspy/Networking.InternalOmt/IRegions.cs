using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.InternalOmt.Dto;

namespace Networking.InternalOmt;

public interface IRegions
{
	Task<Region> GetRegionByRegionPartNumberAsync(string regionPartNumber);

	Task<List<Region>> GetRegionsByContentAsync(string contentPartNumber);

	Task<List<Region>> GetRegionsByReleaseAsync(Identifier releaseIdentifier);
}
