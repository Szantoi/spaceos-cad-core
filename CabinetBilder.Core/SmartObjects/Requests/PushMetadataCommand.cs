using CabinetBilder.Core.SmartObjects;
using MediatR;
using Ardalis.Result;

namespace CabinetBilder.Core.SmartObjects.Requests;

/// <summary>
/// Command to "Push" local metadata changes to the central server.
/// Implements version check to detect conflicts.
/// </summary>
public record PushMetadataCommand(string Handle, SmartObjectMetadata Metadata) : IRequest<Result<SmartObjectSyncResult>>;

