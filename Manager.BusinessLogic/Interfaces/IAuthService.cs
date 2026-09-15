using Manager.BusinessLogic.DTOs;

namespace Manager.BusinessLogic.Interfaces;

// 
// Интерфейс для аутентификационного сервиса, предоставляющий методы для регистрации, входа в систему, восстановления и сброса пароля.
// 
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync (RegisterDto registerDto,CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync (LoginDto loginDto,CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync (ForgotPasswordDto forgotPasswordDto,CancellationToken cancellationToken = default);
    Task ResetPasswordAsync (ResetPasswordDto resetPasswordDto,CancellationToken cancellationToken = default);
}