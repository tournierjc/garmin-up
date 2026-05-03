namespace Networking.Omt.Dto.UniversalMaps;

public class FileCleanupRule
{
	public string? Description { get; init; }

	public string? Path { get; init; }

	public string? ExternalPath { get; init; }

	public string? FileMatch { get; init; }
}
