using Microsoft.VisualStudio.TestTools.UnitTesting;
using CabinetBilder.Core.Machining;

namespace CabinetBilder.Core.Tests.Machining;

[TestClass]
public class GrooveOperationTests
{
    // Typical back-panel groove on a side panel: 600 x 720 x 18 mm
    private const double PanelWidth = 600.0;
    private const double PanelHeight = 720.0;
    private const double PanelThickness = 18.0;

    private static GrooveOperation CreateValidGroove() => new()
    {
        Name = "Backpanel Groove",
        X = 0,
        Y = 700,
        Z = 0,
        Width = 4.0,
        Depth = 10.0,
        Length = 600.0,
        DirectionX = 1,
        DirectionY = 0,
        DirectionZ = 0
    };

    [TestMethod]
    public void Validate_ValidGroove_Succeeds()
    {
        var groove = CreateValidGroove();

        var result = groove.Validate(PanelWidth, PanelHeight, PanelThickness);

        Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
    }

    [TestMethod]
    public void Validate_DepthExceedsThickness_Fails()
    {
        var groove = CreateValidGroove() with { Depth = 20.0 };

        var result = groove.Validate(PanelWidth, PanelHeight, PanelThickness);

        Assert.IsTrue(result.IsFailure);
        StringAssert.Contains(result.ErrorMessage, "thickness");
    }

    [TestMethod]
    public void Validate_ZeroOrNegativeDepth_Fails()
    {
        var groove = CreateValidGroove() with { Depth = 0 };

        var result = groove.Validate(PanelWidth, PanelHeight, PanelThickness);

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void Validate_ThroughGroove_IgnoresDepthAndSucceeds()
    {
        var groove = CreateValidGroove() with { IsThrough = true, Depth = 0 };

        var result = groove.Validate(PanelWidth, PanelHeight, PanelThickness);

        Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
        Assert.AreEqual(PanelThickness, groove.GetEffectiveDepth(PanelThickness), 0.001);
    }

    [TestMethod]
    public void Validate_StartPointOutsidePanel_Fails()
    {
        var groove = CreateValidGroove() with { Y = 800 };

        var result = groove.Validate(PanelWidth, PanelHeight, PanelThickness);

        Assert.IsTrue(result.IsFailure);
        StringAssert.Contains(result.ErrorMessage, "start point");
    }

    [TestMethod]
    public void Validate_EndPointOutsidePanel_Fails()
    {
        // Start inside, but 500 mm groove from X=200 runs past the 600 mm panel edge.
        var groove = CreateValidGroove() with { X = 200, Length = 500 };

        var result = groove.Validate(PanelWidth, PanelHeight, PanelThickness);

        Assert.IsTrue(result.IsFailure);
        StringAssert.Contains(result.ErrorMessage, "end point");
    }

    [TestMethod]
    public void Validate_ZeroDirection_Fails()
    {
        var groove = CreateValidGroove() with { DirectionX = 0, DirectionY = 0, DirectionZ = 0 };

        var result = groove.Validate(PanelWidth, PanelHeight, PanelThickness);

        Assert.IsTrue(result.IsFailure);
        StringAssert.Contains(result.ErrorMessage, "direction");
    }

    [TestMethod]
    public void GetEndPoint_NormalizesDirection()
    {
        // Non-unit direction vector must not stretch the groove length.
        var groove = CreateValidGroove() with { DirectionX = 2, DirectionY = 0, Length = 600 };

        var end = groove.GetEndPoint();

        Assert.AreEqual(600.0, end.X, 0.001);
        Assert.AreEqual(700.0, end.Y, 0.001);
        Assert.AreEqual(0.0, end.Z, 0.001);
    }

    [TestMethod]
    public void GetBottomZ_ReflectsNegativeZCut()
    {
        // Z = 0 is the face, cutting goes towards -Z: bottom = Z - depth.
        var groove = CreateValidGroove();

        Assert.AreEqual(-10.0, groove.GetBottomZ(PanelThickness), 0.001);
    }

    [TestMethod]
    public void GetBottomZ_ThroughGroove_ReachesBackFace()
    {
        var groove = CreateValidGroove() with { IsThrough = true };

        Assert.AreEqual(-PanelThickness, groove.GetBottomZ(PanelThickness), 0.001);
    }
}
