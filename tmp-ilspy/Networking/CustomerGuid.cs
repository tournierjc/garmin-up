using System;

namespace Networking;

public sealed record CustomerGuid(Guid Guid)
{
	public override string ToString()
	{
		return Guid.ToString();
	}

	public string ToString(string format)
	{
		return Guid.ToString(format);
	}
}
