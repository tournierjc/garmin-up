using System.Collections.Generic;

namespace Networking.Connect.Dto.DeviceService;

public record WearableDevice(List<DeviceWeight> DeviceWeights, int WearableDeviceCount);
