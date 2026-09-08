namespace Manager.BusinessLogic.Interfaces;

public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken);
}