using System;

namespace Networking;

internal class NetworkingServiceAttribute(Type interfaceType) : Attribute()
{
	public Type InterfaceType { get; } = interfaceType;
}
