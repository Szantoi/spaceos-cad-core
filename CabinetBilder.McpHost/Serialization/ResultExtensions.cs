using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CabinetBilder.McpHost.Serialization;

public sealed class McpToolResponse<T>
{
    public bool IsSuccess { get; set; }
    public string Status { get; set; } = "Ok";
    public List<string> Errors { get; set; } = new();
    public List<McpValidationError> ValidationErrors { get; set; } = new();
    public T? Value { get; set; }
}

public sealed class McpValidationError
{
    public string Identifier { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public static class ResultExtensions
{
    // Core.Common.Result generic mapping
    public static McpToolResponse<T> ToMcpResponse<T>(this CabinetBilder.Core.Common.Result<T> domainResult)
    {
        return new McpToolResponse<T>
        {
            IsSuccess = domainResult.IsSuccess,
            Status = domainResult.IsSuccess ? "Ok" : "Error",
            Errors = domainResult.IsFailure && !string.IsNullOrEmpty(domainResult.ErrorMessage)
                ? new List<string> { domainResult.ErrorMessage } 
                : new List<string>(),
            Value = domainResult.IsSuccess ? domainResult.Value : default
        };
    }

    // Core.Common.Result non-generic mapping
    public static McpToolResponse<object> ToMcpResponse(this CabinetBilder.Core.Common.Result domainResult)
    {
        return new McpToolResponse<object>
        {
            IsSuccess = domainResult.IsSuccess,
            Status = domainResult.IsSuccess ? "Ok" : "Error",
            Errors = domainResult.IsFailure && !string.IsNullOrEmpty(domainResult.ErrorMessage)
                ? new List<string> { domainResult.ErrorMessage } 
                : new List<string>(),
            Value = null
        };
    }

    // Ardalis.Result generic mapping
    public static McpToolResponse<T> ToMcpResponse<T>(this Ardalis.Result.Result<T> ardalisResult)
    {
        var response = new McpToolResponse<T>
        {
            IsSuccess = ardalisResult.IsSuccess,
            Status = MapArdalisStatus(ardalisResult.Status),
            Errors = ardalisResult.Errors.ToList(),
            Value = ardalisResult.IsSuccess ? ardalisResult.Value : default
        };

        if (ardalisResult.ValidationErrors != null)
        {
            response.ValidationErrors = ardalisResult.ValidationErrors.Select(v => new McpValidationError
            {
                Identifier = v.Identifier,
                ErrorMessage = v.ErrorMessage
            }).ToList();
        }

        return response;
    }

    // Ardalis.Result non-generic mapping
    public static McpToolResponse<object> ToMcpResponse(this Ardalis.Result.Result ardalisResult)
    {
        var response = new McpToolResponse<object>
        {
            IsSuccess = ardalisResult.IsSuccess,
            Status = MapArdalisStatus(ardalisResult.Status),
            Errors = ardalisResult.Errors.ToList(),
            Value = null
        };

        if (ardalisResult.ValidationErrors != null)
        {
            response.ValidationErrors = ardalisResult.ValidationErrors.Select(v => new McpValidationError
            {
                Identifier = v.Identifier,
                ErrorMessage = v.ErrorMessage
            }).ToList();
        }

        return response;
    }

    private static string MapArdalisStatus(Ardalis.Result.ResultStatus status)
    {
        return status switch
        {
            Ardalis.Result.ResultStatus.Ok => "Ok",
            Ardalis.Result.ResultStatus.NotFound => "NotFound",
            Ardalis.Result.ResultStatus.Invalid => "Invalid",
            Ardalis.Result.ResultStatus.Unauthorized => "Unauthorized",
            Ardalis.Result.ResultStatus.Forbidden => "Forbidden",
            _ => "Error"
        };
    }
}
