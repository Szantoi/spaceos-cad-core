using System;
using System.IO;

namespace CabinetBilder.Adapter.AutoCAD.Infrastructure.Catalog;

/// <summary>
/// Provides paths for catalog-related files, ensuring they are in writable locations.
/// </summary>
internal static class CatalogPathProvider
{
    private const string AppFolderName = "CabinetBilder";
    private const string DatabaseFileName = "catalog.db";

    /// <summary>
    /// Gets the path to the writable catalog database file in LocalAppData.
    /// Ensures the directory exists.
    /// </summary>
    public static string GetDatabasePath()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appDataPath = Path.Combine(localAppData, AppFolderName);

        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        return Path.Combine(appDataPath, DatabaseFileName);
    }
}

