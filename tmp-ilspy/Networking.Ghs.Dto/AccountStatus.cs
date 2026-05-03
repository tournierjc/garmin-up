using System;

namespace Networking.Ghs.Dto;

public record AccountStatus(bool Exists, Account? Account, DateTime? LastAccountSyncTime);
