namespace Manager.BusinessLogic.Interfaces;

// 
// Интерфейс для отправки электронных писем, предоставляющий метод для отправки письма с инструкциями по сбросу пароля.
// 
public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken);
}