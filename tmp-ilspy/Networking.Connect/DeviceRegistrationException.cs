using System;

namespace Networking.Connect;

public class DeviceRegistrationException : Exception
{
	public DeviceRegistrationError Error { get; }

	internal DeviceRegistrationException(DeviceRegistrationError error)
	{
		Error = error;
	}
}
