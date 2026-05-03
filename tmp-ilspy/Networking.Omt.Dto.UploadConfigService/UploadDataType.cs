namespace Networking.Omt.Dto.UploadConfigService;

public class UploadDataType
{
	public required string DataTypeName { get; init; }

	public required ExpectedSource[] ExpectedSources { get; init; }

	public required bool ShouldDelete { get; init; }

	public required UploadLocation[] UploadLocations { get; init; }
}
