using System.IO;
using System.Threading.Tasks;
using CabinetBilder.Core.Infrastructure;
using Minio;
using Minio.DataModel.Args;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.Storage;

public class MinioStorageService : IBlobStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;

    public MinioStorageService(IMinioClient minioClient, string bucketName)
    {
        _minioClient = minioClient;
        _bucketName = bucketName;
    }

    public async Task UploadAsync(string fileName, Stream content, string? contentType = null)
    {
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(contentType ?? "application/octet-stream");

        await _minioClient.PutObjectAsync(putObjectArgs);
    }

    public async Task<Stream> DownloadAsync(string fileName)
    {
        var memoryStream = new MemoryStream();
        var getObjectArgs = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithCallbackStream(stream => stream.CopyTo(memoryStream));

        await _minioClient.GetObjectAsync(getObjectArgs);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteAsync(string fileName)
    {
        var removeObjectArgs = new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName);

        await _minioClient.RemoveObjectAsync(removeObjectArgs);
    }

    public async Task<string> GetPresignedUrlAsync(string fileName, int expirySeconds = 3600)
    {
        var presignedGetObjectArgs = new PresignedGetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithExpiry(expirySeconds);

        return await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
    }
}

