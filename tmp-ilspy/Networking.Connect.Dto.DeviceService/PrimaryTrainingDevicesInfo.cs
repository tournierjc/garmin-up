using System.Collections.Generic;

namespace Networking.Connect.Dto.DeviceService;

public record PrimaryTrainingDevicesInfo(List<DeviceWeight> DeviceWeights, int PrimaryTrainingDeviceCount);
