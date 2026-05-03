namespace Networking.Omt.Dto.DlcService;

public class SafetyCamerasActivationResponse
{
	public required string Unlock { get; init; }

	public required byte[] Gma { get; init; }
}
