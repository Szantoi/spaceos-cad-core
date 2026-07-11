using System.IO;
using System.Threading.Tasks;

namespace CabinetBilder.Core.Infrastructure;

public interface IBlobStorageService
{
    Task UploadAsync(string fileName, Stream content, string? contentType = null);
    Task<Stream> DownloadAsync(string fileName);
    Task DeleteAsync(string fileName);
    Task<string> GetPresignedUrlAsync(string fileName, int expirySeconds = 3600);
}

