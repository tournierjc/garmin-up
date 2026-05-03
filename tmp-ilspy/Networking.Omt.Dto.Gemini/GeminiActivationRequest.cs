using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.Gemini;

public class GeminiActivationRequest
{
	public required GeminiMapSectionIdentifier[] MapImages { get; init; }

	public required FullUnitInfo UnitInfo { get; init; }
}
