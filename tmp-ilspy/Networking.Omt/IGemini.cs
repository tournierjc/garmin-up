using System.Threading.Tasks;
using Networking.Omt.Dto.Gemini;

namespace Networking.Omt;

public interface IGemini
{
	Task<GetUpdatesResponse> GetUpdatesAsync();

	Task<ActivateUpdatesResponse> ActivateAsync(GeminiMapSectionIdentifier[] identifiers);
}
