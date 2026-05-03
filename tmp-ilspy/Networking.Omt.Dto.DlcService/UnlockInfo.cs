namespace Networking.Omt.Dto.DlcService;

public class UnlockInfo
{
	public required string PartNumber { get; init; }

	public required string Unlock { get; init; }

	public required byte[] Gma { get; init; }
}
