namespace Networking.Omt.Dto.Common;

public class UrlDto
{
	public UrlType Type { get; set; }

	public string? Url { get; set; }

	public string? Md5 { get; set; }

	public long? Size { get; set; }

	public bool IsRelative { get; set; }
}
