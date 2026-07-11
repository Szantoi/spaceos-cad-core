using CabinetBilder.Core.SmartObjects;
using MediatR;
using Ardalis.Result;

namespace CabinetBilder.Core.SmartObjects.Requests;

/// <summary>
/// Query to check the synchronization status of one or more smart objects against the central server.
/// </summary>
public record CheckSyncStatusQuery(IEnumerable<string> Handles) : IRequest<Result<IEnumerable<SmartObjectSyncResult>>>;

