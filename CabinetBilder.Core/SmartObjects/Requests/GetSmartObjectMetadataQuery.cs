using CabinetBilder.Core.SmartObjects;
using MediatR;
using Ardalis.Result;

namespace CabinetBilder.Core.SmartObjects.Requests;

/// <summary>
/// Query to read and merge metadata for multiple smart objects.
/// </summary>
public record GetSmartObjectMetadataQuery(IEnumerable<string> Handles) 
    : IRequest<Result<SmartObjectMetadata>>;

