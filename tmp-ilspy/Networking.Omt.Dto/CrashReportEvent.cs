using System;

namespace Networking.Omt.Dto;

public class CrashReportEvent
{
	public DateTime TimeStamp { get; }

	public string Message { get; }

	public string UserMessage { get; }

	public byte[] ErrorData { get; }

	public CrashReportEvent(DateTime timeStamp, string message, string userMessage, byte[] errorData)
	{
		TimeStamp = timeStamp;
		Message = message;
		UserMessage = userMessage;
		ErrorData = errorData;
	}
}
