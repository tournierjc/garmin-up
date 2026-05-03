using System.Runtime.CompilerServices;

namespace Networking.Omt.Dto.MarineAccountService;

public record RegisteredDeviceCategory
{
	public string CategoryName { get; init; }

	public RegisteredDevice[] Devices { get; }

	public RegisteredDeviceCategory(string CategoryName, RegisteredDevice[]? Devices)
	{
		this.CategoryName = CategoryName;
		this.Devices = Devices ?? new RegisteredDevice[0];
		base._002Ector();
	}

	[CompilerGenerated]
	public void Deconstruct(out string CategoryName, out RegisteredDevice[]? Devices)
	{
		CategoryName = this.CategoryName;
		Devices = this.Devices;
	}
}
