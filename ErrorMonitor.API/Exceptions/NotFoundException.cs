namespace ErrorMonitor.API.Exceptions;

/// <summary>
/// يُرمى عندما لا يُوجد عنصر مطلوب في قاعدة البيانات.
/// يُرجع تلقائياً 404 Not Found.
/// </summary>
public sealed class NotFoundException(string resource, object id)
    : AppException($"'{resource}' with id '{id}' was not found.", StatusCodes.Status404NotFound);
