using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using CabinetBilder.Core.Sync;
using CabinetBilder.McpHost.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CabinetBilder.McpHost.Tests;

[TestClass]
public class CatalogSeederTests
{
    private static MaterialDto Mat(string code) =>
        new(code, code + " név", "Bútorlap", 18, "{}", 1000m);

    [TestMethod]
    public async Task EmptyCache_SeedsInterimCatalog()
    {
        var store = new Mock<ILocalStore>();
        var empty = (IReadOnlyList<MaterialDto>)new List<MaterialDto>();

        store.SetupSequence(s => s.GetCachedMaterialsAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(Result<IReadOnlyList<MaterialDto>>.Success(empty))
             .ReturnsAsync(Result<IReadOnlyList<MaterialDto>>.Success(CatalogSeeder.InterimMaterials));

        store.Setup(s => s.UpsertMaterialCacheAsync(
                It.IsAny<IReadOnlyList<MaterialDto>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(Result.Success());

        var result = await CatalogSeeder.EnsureSeededAsync(store.Object);

        Assert.IsTrue(result.Count >= 7); // 5 lap/front + 2 élzáró
        store.Verify(s => s.UpsertMaterialCacheAsync(
            It.IsAny<IReadOnlyList<MaterialDto>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task CompleteCache_ReturnsCached_WithoutSeeding()
    {
        var store = new Mock<ILocalStore>();
        // A cache tartalmazza az ÖSSZES interim kódot (+ egy sajátot)
        var full = CatalogSeeder.InterimMaterials.Concat(new[] { Mat("CUSTOM") }).ToList();

        store.Setup(s => s.GetCachedMaterialsAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(Result<IReadOnlyList<MaterialDto>>.Success(full));

        var result = await CatalogSeeder.EnsureSeededAsync(store.Object);

        Assert.AreEqual(full.Count, result.Count);
        store.Verify(s => s.UpsertMaterialCacheAsync(
            It.IsAny<IReadOnlyList<MaterialDto>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task PartialCache_MissingInterimCode_ReSeeds()
    {
        var store = new Mock<ILocalStore>();
        // Régi cache: csak az 5 korábbi anyag, élzárók nélkül (a katalógus bővült!)
        var partial = (IReadOnlyList<MaterialDto>)CatalogSeeder.InterimMaterials.Take(5).ToList();

        store.SetupSequence(s => s.GetCachedMaterialsAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(Result<IReadOnlyList<MaterialDto>>.Success(partial))
             .ReturnsAsync(Result<IReadOnlyList<MaterialDto>>.Success(CatalogSeeder.InterimMaterials));

        store.Setup(s => s.UpsertMaterialCacheAsync(
                It.IsAny<IReadOnlyList<MaterialDto>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(Result.Success());

        var result = await CatalogSeeder.EnsureSeededAsync(store.Object);

        Assert.IsTrue(result.Any(m => m.MaterialCode == "ABS2_WHITE"));
        store.Verify(s => s.UpsertMaterialCacheAsync(
            It.IsAny<IReadOnlyList<MaterialDto>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
