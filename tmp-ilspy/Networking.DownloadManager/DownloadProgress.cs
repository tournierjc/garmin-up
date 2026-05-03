using System;

namespace Networking.DownloadManager;

public class DownloadProgress : IProgress<long>
{
	private DateTime? _startTime;

	private long _existingBytes;

	public long TotalBytes { get; }

	public long CurrentBytes { get; private set; }

	public double PctComplete { get; private set; }

	public double Rate { get; private set; }

	public TimeSpan TimeRemaining { get; private set; }

	protected internal long ExistingBytes
	{
		private get
		{
			return _existingBytes;
		}
		set
		{
			_existingBytes = value;
			CurrentBytes = value;
			if (ExistingBytes == TotalBytes)
			{
				PctComplete = 1.0;
				TimeRemaining = TimeSpan.Zero;
			}
		}
	}

	public DownloadProgress(long totalBytes)
	{
		TotalBytes = totalBytes;
		TimeRemaining = TimeSpan.MaxValue;
	}

	public virtual void Report(long value)
	{
		DateTime valueOrDefault = _startTime.GetValueOrDefault();
		if (!_startTime.HasValue)
		{
			valueOrDefault = DateTime.UtcNow;
			_startTime = valueOrDefault;
		}
		CurrentBytes = value + ExistingBytes;
		PctComplete = (double)CurrentBytes / (double)TotalBytes;
		double totalSeconds = (DateTime.UtcNow - _startTime.Value).TotalSeconds;
		Rate = (double)value / totalSeconds;
		if (totalSeconds < 10.0)
		{
			TimeRemaining = TimeSpan.MaxValue;
		}
		if (CurrentBytes == TotalBytes)
		{
			Rate = 0.0;
			TimeRemaining = TimeSpan.Zero;
		}
		else if (Rate > 0.0)
		{
			TimeRemaining = TimeSpan.FromSeconds((double)(TotalBytes - CurrentBytes) / Rate);
		}
		else
		{
			TimeRemaining = TimeSpan.MaxValue;
		}
	}
}
