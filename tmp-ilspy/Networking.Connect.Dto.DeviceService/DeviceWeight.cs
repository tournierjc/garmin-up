namespace Networking.Connect.Dto.DeviceService;

public record DeviceWeight(long DeviceId, string DisplayName, bool PrimaryTrainingCapable, int Weight);
