using Microsoft.AspNetCore.Mvc;
using Manager.BusinessLogic.DTOs;
using Manager.BusinessLogic.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using Manager.WebAPI.Extensions;


namespace Manager.WebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[EnableRateLimiting("AuthPolicy")] 
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto , CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(registerDto, cancellationToken);
        SetTokenCookie(result.Token);
        return Ok(ApiResponse<AuthResultPublicDto>.Ok(new AuthResultPublicDto(result.Email), "Регистрация прошла успешно."));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(loginDto , cancellationToken);
        SetTokenCookie(result.Token);
        return Ok(ApiResponse<AuthResultPublicDto>.Ok(new AuthResultPublicDto(result.Email), "Авторизация прошла успешно."));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(dto, cancellationToken);
        return Ok(ApiResponse.Ok("Инструкции по сбросу пароля отправлены на ваш email."));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(dto, cancellationToken);
        return Ok(ApiResponse.Ok("Пароль успешно сброшен."));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("jwt");
        return Ok(ApiResponse.Ok("Выход из системы прошел успешно."));
    }

    private void SetTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // Запрещает доступ к куке из JavaScript (защита от XSS)
            Secure = true,   // Кука передается только по HTTPS (обязательно для прода!)
            SameSite = SameSiteMode.None, // Защита от CSRF-атак
            Expires = DateTime.UtcNow.AddDays(7) // Совпадает со временем жизни токена
        };

        Response.Cookies.Append("jwt", token, cookieOptions);
    }
}