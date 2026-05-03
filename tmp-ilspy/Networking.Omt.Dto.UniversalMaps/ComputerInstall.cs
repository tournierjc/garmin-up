namespace Networking.Omt.Dto.UniversalMaps;

public class ComputerInstall
{
	public required string Manifest { get; init; }

	public required MapInstallIdentifier Identifier { get; init; }
}
