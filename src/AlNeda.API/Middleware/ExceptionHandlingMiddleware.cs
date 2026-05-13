using System.Net;
using System.Text.Json;
using AlNeda.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
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
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "تعارض تزامن عند تحديث السجلات");
            await WriteErrorResponse(context, HttpStatusCode.Conflict,
                "تم تعديل السجل من قبل مستخدم آخر. أعد تحميل الصفحة وحاول مرة أخرى.", "DB_CONCURRENCY");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "خطأ في حفظ البيانات (EF)");
            await WriteErrorResponse(context, HttpStatusCode.BadRequest,
                "تعذر حفظ البيانات. قد تكون هناك بيانات مكررة أو مراجع غير صالحة.", "DB_UPDATE");
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex)
        {
            _logger.LogError(ex, "خطأ SQLite: {SqliteErrorCode}", ex.SqliteErrorCode);
            var (status, message, code) = MapSqlite(ex);
            await WriteErrorResponse(context, status, message, code);
        }
        catch (System.Data.Common.DbException ex)
        {
            _logger.LogError(ex, "خطأ في قاعدة البيانات");
            await WriteErrorResponse(context, HttpStatusCode.ServiceUnavailable,
                "تعذر الاتصال بقاعدة البيانات أو تنفيذ العملية", "DB_ERROR");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, "يجب تسجيل الدخول", "AUTH_REQUIRED");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            await WriteErrorResponse(context, HttpStatusCode.NotFound, "المورد المطلوب غير موجود", "ORDER_NOT_FOUND");
        }
        catch (TaskCanceledException)
        {
            await WriteErrorResponse(context, HttpStatusCode.RequestTimeout, "انتهت مهلة الطلب", "SERVER_ERROR");
        }
        catch (OperationCanceledException)
        {
            await WriteErrorResponse(context, HttpStatusCode.RequestTimeout, "تم إلغاء الطلب", "SERVER_ERROR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "حدث خطأ في الخادم", "SERVER_ERROR");
        }
    }

    /// <summary>
    /// لا نعرض رسائل SQLite الداخلية للعميل.
    /// </summary>
    private static (HttpStatusCode Status, string Message, string Code) MapSqlite(Microsoft.Data.Sqlite.SqliteException ex)
    {
        // قفل / مشغول
        if (ex.SqliteErrorCode == 5 || ex.SqliteErrorCode == 6)
            return (HttpStatusCode.ServiceUnavailable, "قاعدة البيانات مشغولة مؤقتاً. حاول بعد قليل.", "DB_BUSY");
        // قيود / NOT NULL / UNIQUE
        if (ex.SqliteErrorCode == 19)
            return (HttpStatusCode.BadRequest, "بيانات غير متوافقة مع قيود قاعدة البيانات", "DB_CONSTRAINT");
        return (HttpStatusCode.ServiceUnavailable, "تعذر الاتصال بقاعدة البيانات", "DB_ERROR");
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message, string code)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(new ApiError
        {
            Message = message,
            Code = code,
            Details = new { }
        }, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}
