namespace Networking.Omt.Dto.ApplicationService;

public class NotificationMessage
{
	public string? Title { get; set; }

	public string? Message { get; set; }

	public long? UnitId { get; set; }

	public NotificationMessageType Type { get; set; }

	public NotificationLevel Level { get; set; }

	public string? LinkText { get; set; }

	public string? LinkUrl { get; set; }
}
