using System.Security.Claims;
using Manager.BusinessLogic.DTOs;
using Manager.BusinessLogic.Interfaces;
using Manager.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Manager.DataAccess.Interfaces;
using Manager.BusinessLogic.Exceptions;
using Microsoft.EntityFrameworkCore;



namespace Manager.BusinessLogic.Services;

public class AuthService : IAuthService
{
    private const string DefaultRole = "Employee";
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;

    public AuthService(UserManager<User> userManager,  IConfiguration configuration, IUnitOfWork unitOfWork, IEmailSender emailSender)
    {
        _userManager = userManager;
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default)
    {
        var authUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (authUser != null)
        {
            throw new ConflictException("Не удалось зарегистрировать пользователя с указанными данными.");
        }
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try{
        
        var user = new User
        {
            Email = registerDto.Email,
            UserName = registerDto.Email,
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ValidationAppException($"Ошибка при создании пользователя: {errors}");
        }

         var roleResult = await _userManager.AddToRoleAsync(user, DefaultRole);
         if (!roleResult.Succeeded)
         {
             var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
             throw new ValidationAppException($"Ошибка при добавлении роли пользователю: {errors}");
         }

         var employee = new Employee
         {
             Name = registerDto.FirstName,
             LastName = registerDto.LastName,
             UserId = user.Id
         };
         await _unitOfWork.Employees.AddAsync(employee, cancellationToken);
         await _unitOfWork.SaveChangesAsync(cancellationToken);

         var token = await GenerateJwtToken(user , employee.Id);

         await _unitOfWork.CommitTransactionAsync(cancellationToken);

         return new AuthResponseDto {
            Token = token,
            Email = user.Email
         };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null)
        {
            throw new AuthenticationFailedException();
        }

    
        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new AuthenticationFailedException("Аккаунт заблокирован. Попробуйте позже.");
        }
       
         var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);

         if (!isPasswordValid)
         {
             await _userManager.AccessFailedAsync(user);
            
            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new AuthenticationFailedException("Аккаунт заблокирован из-за слишком большого количества неудачных попыток входа. Попробуйте позже.");
            }
            throw new AuthenticationFailedException();
         }

         await _userManager.ResetAccessFailedCountAsync(user);

        var employee = await _unitOfWork.Employees.GetAll()
              .FirstOrDefaultAsync(e => e.UserId == user.Id , cancellationToken);

        if(employee == null)
        {
            throw new NotFoundException("Сотрудник не найден. Обратитесь в поддержку");
        }

        var token = await GenerateJwtToken(user , employee.Id);

        return new AuthResponseDto
        {
            Token = token,
            Email = user.Email!
        };
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);

        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] 
        ?? throw new InvalidOperationException("FrontendBaseUrl не сконфигурирован.");

        var resetLink = $"{frontendBaseUrl}/reset-password?email={Uri.EscapeDataString(dto.Email)}&token={encodedToken}";

        await _emailSender.SendPasswordResetEmailAsync(dto.Email, resetLink, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if(user == null)
        {
            throw new ValidationAppException("Пользователь с указанным email не найден.");
        }

        var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ValidationAppException($"Ошибка при сбросе пароля: {errors}");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

    }

    private async Task<string> GenerateJwtToken(User user, int employeeId)
    {

        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name , $"{user.UserName}"),
            new Claim("EmployeeId", employeeId.ToString()),
        };

        foreach(var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var jwtKey = _configuration["Jwt:Key"] ??
        throw new InvalidOperationException("Jwt:Key не сконфигурирован.");

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}