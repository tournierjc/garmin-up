using System.Globalization;

namespace Networking.Omt.Dto.ApplicationService;

public record ApplicationUpdateRequest(string ApplicationName, string CurrentVersion)
{
	public object ClientInfo => new
	{
		LocaleCode = CultureInfo.CurrentCulture.Name
	};
}
