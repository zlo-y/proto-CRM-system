using Manager.BusinessLogic.Interfaces;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using Manager.BusinessLogic.Models;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Manager.BusinessLogic.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> emailSettings, ILogger<SmtpEmailSender> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName , _emailSettings.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Password Reset Request";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $"<p>You requested a password reset. Click the link below to reset your password:</p><p><a href='{resetLink}'>Reset Password</a></p>"
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.Auto, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_emailSettings.Username))
            {
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Password reset email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
            throw;
        }
    
    }
}