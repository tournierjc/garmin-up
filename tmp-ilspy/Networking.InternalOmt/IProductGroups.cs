using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.InternalOmt.Dto;

namespace Networking.InternalOmt;

public interface IProductGroups
{
	Task<List<ProductGroup>> GetProductGroupsAsync();

	Task<ProductGroup> GetProductGroupByGroupCodeAsync(string groupCode);
}
