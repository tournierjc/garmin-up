using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.InternalOmt.Dto;

namespace Networking.InternalOmt;

public interface IContents
{
	Task<Content> GetContentAsync(string contentPartNumber);

	Task<List<Content>> GetContentAdditionsAsync(string contentPartNumber, string? productGroupCode = null);

	Task<List<Content>> GetContentsByReleaseAsync(Identifier releaseIdentifier);

	Task<List<string>> GetContentsOnDeviceAsync();
}
