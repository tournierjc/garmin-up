using System.Threading.Tasks;
using Networking.Omt.Dto;

namespace Networking.Omt.Analytics;

public interface IAnalytics
{
	Task SendCrashReportEventAsync(CrashReportEvent crashReportEvent);
}
