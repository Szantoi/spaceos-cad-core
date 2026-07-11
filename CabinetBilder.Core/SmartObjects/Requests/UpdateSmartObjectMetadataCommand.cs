using CabinetBilder.Core.SmartObjects;
using MediatR;
using Ardalis.Result;

namespace CabinetBilder.Core.SmartObjects.Requests;

/// <summary>
/// Command to update metadata for multiple smart objects.
/// </summary>
public record UpdateSmartObjectMetadataCommand(IEnumerable<string> Handles, SmartObjectMetadata Metadata) 
    : IRequest<Result>;

