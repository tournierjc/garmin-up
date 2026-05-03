namespace Networking;

public sealed class Optional<T> where T : class
{
	public T? Value { get; }

	public Optional(T? value)
	{
		Value = value;
	}
}
