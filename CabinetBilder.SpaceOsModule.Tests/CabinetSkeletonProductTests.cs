using System;
using System.Linq;
using System.Threading.Tasks;
using CabinetBilder.Core.Skeletons;
using CabinetBilder.SpaceOsModule;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.SpaceOsModule.Tests;

[TestClass]
public class CabinetSkeletonProductTests
{
    private static CabinetSkeletonProduct MakeProduct(Guid tenantId)
    {
        var skeleton = new Skeleton(SkeletonId.New());
        return new CabinetSkeletonProduct(skeleton, tenantId);
    }

    [TestMethod]
    public void ProductId_MatchesSkeletonId()
    {
        var skeletonId = SkeletonId.New();
        var skeleton = new Skeleton(skeletonId);
        var product = new CabinetSkeletonProduct(skeleton, Guid.NewGuid());

        Assert.AreEqual(skeletonId.Value, product.ProductId);
    }

    [TestMethod]
    public void Parameters_ExposesSkeletonParametersAsDictionary()
    {
        var product = MakeProduct(Guid.NewGuid());

        Assert.IsTrue(product.Parameters.ContainsKey("Width"));
        Assert.AreEqual(600.0, product.Parameters["Width"]);
    }

    [TestMethod]
    public async Task GenerateGeometry_ReturnsFivePanelsForDefaultSkeleton()
    {
        var product = MakeProduct(Guid.NewGuid());

        var result = await product.GenerateGeometry(engine: null!);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(5, result.Primitives.Count); // Side Left, Side Right, Bottom, Top, Back
        Assert.IsTrue(result.Primitives.Any(p => p.Name == "Side Left"));
        Assert.IsTrue(result.Primitives.Any(p => p.Name == "Back"));
    }

    [TestMethod]
    public async Task ValidateParameters_ValidForDefaultSkeleton()
    {
        var product = MakeProduct(Guid.NewGuid());

        var result = await product.ValidateParameters();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public async Task ValidateParameters_InvalidWhenThicknessTooLargeForWidth()
    {
        var product = MakeProduct(Guid.NewGuid());
        product.ApplyParameter("Width", 20.0); // thickness (18) * 2 >= 20

        var result = await product.ValidateParameters();

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Thickness")));
    }

    [TestMethod]
    public async Task ApplyParameter_ThenGenerateGeometry_ReflectsUpdatedWidth()
    {
        var product = MakeProduct(Guid.NewGuid());
        product.ApplyParameter("Width", 800.0);

        var result = await product.GenerateGeometry(engine: null!);

        var right = result.Primitives.Single(p => p.Name == "Side Right");
        Assert.AreEqual(800.0 - 18.0, right.PosX, 0.0001); // PosX = Width - Thickness
    }
}
