using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using CabinetBilder.SpaceOsBridge.Outbox;
using CabinetBilder.Core.Sync;
using Microsoft.Extensions.Logging;
using Ardalis.Result;
using System.Text;

namespace CabinetBilder.Tests.Outbox;

[TestClass]
public class OutboxQueueTests
{
    private Mock<ILocalStore> _storeMock;
    private Mock<ISecurityService> _securityMock;
    private Mock<ILogger<OutboxQueue>> _loggerMock;
    private OutboxQueue _queue;

    [TestInitialize]
    public void Setup()
    {
        _storeMock = new Mock<ILocalStore>();
        _securityMock = new Mock<ISecurityService>();
        _loggerMock = new Mock<ILogger<OutboxQueue>>();
        _queue = new OutboxQueue(_storeMock.Object, _securityMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    public async Task EnqueueAsync_LargePayload_ReturnsError()
    {
        // Arrange
        var largePayload = new string('a', 1024 * 1024 + 1); // > 1MB

        // Act
        var result = await _queue.EnqueueAsync(OutboxOperation.UploadBom, largePayload);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("exceeds the 1MB limit")));
    }

    [TestMethod]
    public async Task EnqueueAsync_SensitivePayload_EncryptsAndSetsJsonToNull()
    {
        // Arrange
        var payload = "{\"data\":\"secret\"}";
        var encrypted = Encoding.UTF8.GetBytes("encrypted-data");
        _securityMock.Setup(s => s.ProtectStringAsync(payload, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(encrypted));
        
        _storeMock.Setup(s => s.EnqueueOutboxAsync(It.IsAny<OutboxEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Guid.NewGuid()));

        // Act
        var result = await _queue.EnqueueAsync(OutboxOperation.UploadBom, payload, isSensitive: true);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _storeMock.Verify(s => s.EnqueueOutboxAsync(
            It.Is<OutboxEntry>(e => e.PayloadJson == null && e.EncryptedPayloadDpapi == encrypted), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task EnqueueAsync_NormalPayload_SetsJsonAndEncryptedToNull()
    {
        // Arrange
        var payload = "{\"data\":\"normal\"}";
        _storeMock.Setup(s => s.EnqueueOutboxAsync(It.IsAny<OutboxEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Guid.NewGuid()));

        // Act
        var result = await _queue.EnqueueAsync(OutboxOperation.UploadBom, payload, isSensitive: false);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        _storeMock.Verify(s => s.EnqueueOutboxAsync(
            It.Is<OutboxEntry>(e => e.PayloadJson == payload && e.EncryptedPayloadDpapi == null), 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
