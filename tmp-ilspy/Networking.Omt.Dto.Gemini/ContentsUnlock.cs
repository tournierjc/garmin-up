namespace Networking.Omt.Dto.Gemini;

public class ContentsUnlock
{
	public required string[] PartNumbers { get; init; }

	public required byte[] Gma { get; init; }
}
