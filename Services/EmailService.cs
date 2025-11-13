using System.Net;
using System.Net.Mail;
using E_commerce_Application.Configuration;
using E_commerce_Application.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace E_commerce_Application.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) ||
            string.IsNullOrWhiteSpace(_settings.FromAddress) ||
            string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("Email not sent. Missing configuration or recipient. To: {Recipient}", to);
            return;
        }

        using var smtpClient = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_settings.Username) && !string.IsNullOrWhiteSpace(_settings.Password))
        {
            smtpClient.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName ?? _settings.FromAddress),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        message.To.Add(to);

        try
        {
            using var registration = cancellationToken.Register(() => smtpClient.SendAsyncCancel());
            await smtpClient.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent to {Recipient} with subject {Subject}", to, subject);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Email sending cancelled for recipient {Recipient}", to);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", to);
        }
    }
}


