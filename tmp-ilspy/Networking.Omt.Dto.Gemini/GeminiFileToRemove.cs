namespace Networking.Omt.Dto.Gemini;

public class GeminiFileToRemove
{
	public required string? FileName { get; init; }

	public required string? PartNumber { get; init; }

	public required long SizeInBytes { get; init; }
}
