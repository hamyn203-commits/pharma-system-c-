using System.Net;
using System.Text.Json;

namespace AlNeda.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (DbException ex)
        {
            _logger.LogError(ex, "Database error occurred");
            await WriteErrorResponse(context, HttpStatusCode.ServiceUnavailable,
                "عذراً، حدث خطأ في الاتصال بقاعدة البيانات");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            await WriteErrorResponse(context, HttpStatusCode.Unauthorized,
                "ليس لديك صلاحية للوصول إلى هذا المورد");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            await WriteErrorResponse(context, HttpStatusCode.NotFound,
                "المورد المطلوب غير موجود");
        }
        catch (TaskCanceledException)
        {
            await WriteErrorResponse(context, HttpStatusCode.RequestTimeout,
                "انتهت مهلة الطلب، يرجى المحاولة مرة أخرى");
        }
        catch (OperationCanceledException)
        {
            await WriteErrorResponse(context, HttpStatusCode.RequestTimeout,
                "تم إلغاء الطلب");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError,
                "عذراً، حدث خطأ في الخادم");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            message,
            statusCode = (int)statusCode,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response, _jsonOptions);
        await context.Response.WriteAsync(json);
    }
}

// Custom exception to wrap known DB errors so the middleware can catch them
public class DbException : Exception
{
    public DbException(string message, Exception inner) : base(message, inner) { }
}
