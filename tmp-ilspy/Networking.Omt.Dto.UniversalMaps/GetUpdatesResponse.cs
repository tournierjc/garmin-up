using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.UniversalMaps;

public class GetUpdatesResponse
{
	public required UniversalMap[] Maps { get; init; }

	public required FileToRemove[] FilesToRemove { get; init; }

	public required FileCleanupRule[] DirectoriesToRemove { get; init; }

	public required BundledMap[] BundledMaps { get; init; }
}
