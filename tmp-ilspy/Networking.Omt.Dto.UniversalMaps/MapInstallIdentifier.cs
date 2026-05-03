namespace Networking.Omt.Dto.UniversalMaps;

public record MapInstallIdentifier
{
	public string? RegionPartNumber { get; init; }

	public string? MapImagePartNumber { get; init; }

	public string? ActivationRequestCode { get; init; }
}
