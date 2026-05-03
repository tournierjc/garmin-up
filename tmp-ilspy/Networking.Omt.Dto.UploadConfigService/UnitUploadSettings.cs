namespace Networking.Omt.Dto.UploadConfigService;

public class UnitUploadSettings
{
	public required UploadDataType[] DataTypes { get; init; }

	public required string UnitTypeName { get; init; }
}
