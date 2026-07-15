using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CabinetBilder.Core.Machining;
using CabinetBilder.Core.Skeletons;

namespace CabinetBilder.Core.Tests.Skeletons;

[TestClass]
public class GroovingTests
{
    private static readonly string[] CarcassPanels = { "Side Left", "Side Right", "Bottom", "Top" };

    private static GrooveOperation? GrooveOf(Skeleton s, string panelName) =>
        s.Components.Single(c => c.Name == panelName)
                    .Operations.OfType<GrooveOperation>().SingleOrDefault();

    [TestMethod]
    public void DefaultAppliedBack_ProducesNoGrooves()
    {
        // Default BackGrooved = false → applied back, carcass stays ungrooved.
        var skeleton = new Skeleton(SkeletonId.New());

        var grooveCount = skeleton.Components
            .SelectMany(c => c.Operations.OfType<GrooveOperation>())
            .Count();

        Assert.AreEqual(0, grooveCount);
    }

    [TestMethod]
    public void GroovedBack_AddsExactlyOneGrooveToEachCarcassPanel()
    {
        var skeleton = new Skeleton(SkeletonId.New());

        var result = skeleton.ApplyParameter("BackGrooved", true);

        Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
        foreach (var name in CarcassPanels)
        {
            Assert.IsNotNull(GrooveOf(skeleton, name), $"{name} should have a back-panel groove.");
        }
    }

    [TestMethod]
    public void GroovedBack_BackPanelItselfIsNotGrooved()
    {
        var skeleton = new Skeleton(SkeletonId.New());
        skeleton.ApplyParameter("BackGrooved", true);

        var back = skeleton.Components.Single(c => c.Name == "Back");

        Assert.AreEqual(0, back.Operations.OfType<GrooveOperation>().Count());
    }

    [TestMethod]
    public void GroovedBack_SideGroove_RunsVerticallyTheFullHeightAtRearSetback()
    {
        var skeleton = new Skeleton(SkeletonId.New());
        skeleton.ApplyParameter("BackGrooved", true);

        var side = skeleton.Components.Single(c => c.Name == "Side Left");
        var groove = GrooveOf(skeleton, "Side Left")!;

        // Vertical: runs along local +Y for the full panel height.
        Assert.AreEqual(0, groove.DirectionX, 0.001);
        Assert.AreEqual(1, groove.DirectionY, 0.001);
        Assert.AreEqual(side.Height, groove.Length, 0.001);
        // Positioned one setback in from the rear edge (rear = local X == Width).
        Assert.AreEqual(side.Width - 12.0, groove.X, 0.001);
    }

    [TestMethod]
    public void GroovedBack_BottomGroove_RunsHorizontallyTheFullWidthAtRearSetback()
    {
        var skeleton = new Skeleton(SkeletonId.New());
        skeleton.ApplyParameter("BackGrooved", true);

        var bottom = skeleton.Components.Single(c => c.Name == "Bottom");
        var groove = GrooveOf(skeleton, "Bottom")!;

        // Horizontal: runs along local +X for the full panel width.
        Assert.AreEqual(1, groove.DirectionX, 0.001);
        Assert.AreEqual(0, groove.DirectionY, 0.001);
        Assert.AreEqual(bottom.Width, groove.Length, 0.001);
        // Positioned one setback in from the rear edge (rear = local Y == Height).
        Assert.AreEqual(bottom.Height - 12.0, groove.Y, 0.001);
    }

    [TestMethod]
    public void GroovedBack_GrooveWidthIsBackThicknessPlusClearance()
    {
        var skeleton = new Skeleton(SkeletonId.New());
        skeleton.ApplyParameter("BackGrooved", true);

        var back = skeleton.Components.Single(c => c.Name == "Back");
        var groove = GrooveOf(skeleton, "Side Left")!;

        Assert.AreEqual(back.Thickness + 0.2, groove.Width, 0.001);
    }

    [TestMethod]
    public void GroovedBack_GrooveDepthMatchesParameter()
    {
        var skeleton = new Skeleton(SkeletonId.New());
        skeleton.ApplyParameter("BackGrooved", true);

        var groove = GrooveOf(skeleton, "Top")!;

        Assert.AreEqual(8.0, groove.Depth, 0.001);
    }

    [TestMethod]
    public void GroovedBack_EveryGrooveValidatesAgainstItsHostPanel()
    {
        var skeleton = new Skeleton(SkeletonId.New());
        skeleton.ApplyParameter("BackGrooved", true);

        foreach (var name in CarcassPanels)
        {
            var panel = skeleton.Components.Single(c => c.Name == name);
            var groove = GrooveOf(skeleton, name)!;

            var result = groove.Validate(panel.Width, panel.Height, panel.Thickness);

            Assert.IsTrue(result.IsSuccess, $"{name}: {result.ErrorMessage}");
        }
    }

    [TestMethod]
    public void TogglingBackGroovedOff_RemovesTheGroovesOnRebuild()
    {
        var skeleton = new Skeleton(SkeletonId.New());
        skeleton.ApplyParameter("BackGrooved", true);
        Assert.IsNotNull(GrooveOf(skeleton, "Side Left"));

        skeleton.ApplyParameter("BackGrooved", false);

        var grooveCount = skeleton.Components
            .SelectMany(c => c.Operations.OfType<GrooveOperation>())
            .Count();
        Assert.AreEqual(0, grooveCount);
    }
}
