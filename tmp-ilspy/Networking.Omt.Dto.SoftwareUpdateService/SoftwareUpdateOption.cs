using System.Collections.Generic;
using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.SoftwareUpdateService;

public class SoftwareUpdateOption
{
	public List<string>? Changes { get; set; }

	public string? DisplayName { get; set; }

	public string? EulaUrl { get; set; }

	public string? FilePathOnUnit { get; set; }

	public bool IsRecommended { get; set; }

	public UrlDto? Url { get; set; }

	public bool IsRestartRequired { get; set; }

	public string? PartNumber { get; set; }

	public string? SoftwareVersion { get; set; }

	public bool IsPrimaryFirmware { get; set; }

	public string? Locale { get; set; }

	public ChangeSeverity ChangeSeverity { get; set; }

	public bool IsReinstall { get; set; }

	public string? DataType { get; set; }

	public string? InstallationOrder { get; set; }
}
