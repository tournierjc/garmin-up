using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.ApplicationService;

public class ApplicationUpdateResponse
{
	public string? VersionNumber { get; set; }

	public bool IsRequiredUpdate { get; set; }

	public bool CanInstallSilently { get; set; }

	public UrlDto? FileUrl { get; set; }
}
