using System.Threading;
using System.Threading.Tasks;

namespace Networking.DownloadManager;

public interface IDownloadManager
{
	Task DownloadFileAsync(Downloadable downloadable, CancellationToken cancellationToken);
}
