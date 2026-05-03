namespace Networking.Omt.Dto.Common;

public class UnitUpdateFile
{
	public string? FileName { get; set; }

	public int MajorVersion { get; set; }

	public int MinorVersion { get; set; }

	public string? PartNumber { get; set; }

	public string? Path { get; set; }
}
