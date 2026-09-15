namespace Manager.WebAPI.Extensions;

// 
// Модель для формирования стандартного ответа API, содержащего информацию об успешности операции, сообщении и данных.
// 
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> Ok (T data, string? message = null) => new()
    {
        Success = true,
        Message = message,
        Data = data
    };

    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false,
        Message = message,
        Data = default
    };
    

        
}