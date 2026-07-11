using CabinetBilder.Core.SmartObjects;
using MediatR;
using Ardalis.Result;

namespace CabinetBilder.Core.SmartObjects.Requests;

/// <summary>
/// Command to "Pull" metadata from the central server to the local AutoCAD drawing.
/// This will overwrite local changes!
/// </summary>
public record PullMetadataCommand(string Handle) : IRequest<Result<SmartObjectSyncResult>>;

