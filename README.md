# CabinetBilder AutoCAD Scripts (.NET 10)

This repository contains a .NET 10 AutoCAD plugin project for small productivity commands.

## Project Structure

- `CabinetBilder.AutoCadScripts.slnx` - solution file
- `App.AutoCadScripts/` - AutoCAD plugin class library
- `App.AutoCadScripts/Commands/DimensionCommands.cs` - `SyncDimToBlock` command
- `docs/Architecture_Patterns_Collection.md` - architecture and design pattern collection

## Documentation

- [Architecture and design pattern collection](docs/Architecture_Patterns_Collection.md)

## Prerequisites

1. AutoCAD installed (example path: `C:\Program Files\Autodesk\AutoCAD 2025`)
2. .NET SDK 10+ installed

## Configure AutoCAD Managed DLL Path

The project expects the following DLL files in `AutoCADManagedDllPath`:

- `acdbmgd.dll`
- `accoremgd.dll`
- `acmgd.dll`

By default, the project uses:

- `C:\Program Files\Autodesk\AutoCAD 2025`

If your AutoCAD is installed elsewhere, build with:

```powershell
dotnet build .\CabinetBilder.AutoCadScripts.slnx -p:AutoCADManagedDllPath="D:\Apps\Autodesk\AutoCAD 2024"
```

## Build

```powershell
dotnet build .\CabinetBilder.AutoCadScripts.slnx
```

Note: The project downgrades `MSB3277` to message level because AutoCAD managed assemblies can introduce expected reference-version conflicts during design-time/build resolution.

## Load in AutoCAD

1. Start AutoCAD
2. Run `NETLOAD`
3. Select compiled DLL:
   - `App.AutoCadScripts\bin\Debug\net10.0\App.AutoCadScripts.dll`
4. Run command:
   - `SyncDimToBlock`

## Command Behavior

`SyncDimToBlock`:

1. Select a dimension
2. Select a dynamic block reference
3. Updates dynamic property named `Távolság` with the dimension measurement

If your block property uses a different name, update `DefaultDistancePropertyName` in:

- `App.AutoCadScripts/Commands/DimensionCommands.cs`
