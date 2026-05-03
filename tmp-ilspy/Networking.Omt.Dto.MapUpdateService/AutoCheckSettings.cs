using System;

namespace Networking.Omt.Dto.MapUpdateService;

public class AutoCheckSettings
{
	public int MinimumDaysBetweenChecks { get; set; }

	public DateTime? DisableChecksUntil { get; set; }

	public bool IsAutoCheckEnabled { get; set; }
}
