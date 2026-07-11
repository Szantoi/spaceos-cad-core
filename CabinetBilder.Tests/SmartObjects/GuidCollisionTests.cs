using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using CabinetBilder.Adapter.AutoCAD.Infrastructure.ObjectMetadata;
using CabinetBilder.Core.Sync;
using Microsoft.Extensions.Logging;
using CabinetBilder.Core.SmartObjects;
using System;
using System.Collections.Generic;

namespace CabinetBilder.Tests.SmartObjects;

[TestClass]
public class GuidCollisionTests
{
    private Mock<ILocalStore> _localStoreMock;
    private Mock<ILogger<DrawingOpenedHandler>> _loggerMock;

    [TestInitialize]
    public void Setup()
    {
        _localStoreMock = new Mock<ILocalStore>();
        _loggerMock = new Mock<ILogger<DrawingOpenedHandler>>();
    }

    [TestMethod]
    public void DrawingOpenedHandler_InitializesCorrectly()
    {
        // Use null for IDrawingObjectMetadataStore to avoid Moq proxying issues with AutoCAD types
        var handler = new DrawingOpenedHandler(null!, _localStoreMock.Object, _loggerMock.Object);
        Assert.IsNotNull(handler);
    }
}
