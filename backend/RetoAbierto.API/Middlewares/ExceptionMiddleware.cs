using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using RetoAbierto.Application.Common.Exceptions;

namespace RetoAbierto.API.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocurrió una excepción capturada por el Middleware global.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        int statusCode;
        string title;
        string detail;
        IDictionary<string, string[]>? errors = null;

        switch (exception)
        {
            case AppValidationException valEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                title = "Error de validación";
                detail = valEx.Message;
                errors = valEx.Errors;
                break;

            case ValidationException fluentValEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                title = "Error de validación";
                detail = "Uno o más campos no son válidos.";
                errors = fluentValEx.Errors
                    .GroupBy(e => char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName.Substring(1), e => e.ErrorMessage)
                    .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
                break;

            case ConflictException conflictEx:
                statusCode = (int)HttpStatusCode.Conflict;
                title = "Conflicto de negocio";
                detail = conflictEx.Message;
                break;

            case NotFoundException notFoundEx:
                statusCode = (int)HttpStatusCode.NotFound;
                title = "Recurso no encontrado";
                detail = notFoundEx.Message;
                break;

            case ForbiddenException forbiddenEx:
                statusCode = (int)HttpStatusCode.Forbidden;
                title = "Acceso prohibido";
                detail = forbiddenEx.Message;
                break;

            case UnauthorizedAccessException unauthEx:
                statusCode = (int)HttpStatusCode.Unauthorized;
                title = "No autorizado";
                detail = unauthEx.Message;
                break;

            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                title = "Error interno del servidor";
                detail = "Ocurrió un error inesperado al procesar su solicitud.";
                break;
        }

        context.Response.StatusCode = statusCode;

        var responsePayload = new ErrorResponseDto
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Errors = errors
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(responsePayload, jsonOptions));
    }
}

public class ErrorResponseDto
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;

    [JsonPropertyName("errors")]
    public IDictionary<string, string[]>? Errors { get; set; }
}

