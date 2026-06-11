using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace TeacherApp.Api.Infrastructure.Email;

public sealed class SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly SmtpOptions _smtp = options.Value;

    public async Task SendStudentWelcomeEmailAsync(
        string toEmail, string studentName, string provisionalPassword, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
        message.To.Add(new MailboxAddress(studentName, toEmail));
        message.Subject = "Bem-vindo à DhSchool - Seus dados de acesso";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = BuildWelcomeHtml(studentName, toEmail, provisionalPassword),
            TextBody = BuildWelcomeText(studentName, toEmail, provisionalPassword)
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var secureSocket = _smtp.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await client.ConnectAsync(_smtp.Host, _smtp.Port, secureSocket, ct);
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password, ct);
            await client.SendAsync(message, ct);
            logger.LogInformation("Welcome email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }

    private static string BuildWelcomeHtml(string name, string email, string password)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"></head>
            <body style="font-family: 'Segoe UI', Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">
                <div style="background: linear-gradient(135deg, #512BD4, #2B0B98); padding: 32px; border-radius: 16px 16px 0 0; text-align: center;">
                    <h1 style="color: white; margin: 0; font-size: 28px;">DhSchool</h1>
                    <p style="color: rgba(255,255,255,0.8); margin: 8px 0 0;">Welcome to your learning journey</p>
                </div>
                <div style="background: #f8f9fa; padding: 32px; border-radius: 0 0 16px 16px; border: 1px solid #e9ecef; border-top: none;">
                    <h2 style="color: #333; margin-top: 0;">Olá, {System.Net.WebUtility.HtmlEncode(name)}!</h2>
                    <p>Sua conta foi criada com sucesso. Use as credenciais abaixo para acessar o aplicativo:</p>
                    <div style="background: white; border: 1px solid #dee2e6; border-radius: 12px; padding: 20px; margin: 20px 0;">
                        <p style="margin: 4px 0;"><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(email)}</p>
                        <p style="margin: 4px 0;"><strong>Senha:</strong> <code style="background: #f1f3f5; padding: 2px 8px; border-radius: 4px; font-size: 14px;">{System.Net.WebUtility.HtmlEncode(password)}</code></p>
                    </div>
                    <p style="color: #6c757d; font-size: 13px;">Recomendamos que você altere sua senha após o primeiro acesso.</p>
                    <hr style="border: none; border-top: 1px solid #e9ecef; margin: 24px 0;">
                    <p style="color: #999; font-size: 12px; text-align: center;">
                        Este email foi enviado automaticamente pela DhSchool.<br>
                        Não responda a este email.
                    </p>
                </div>
            </body>
            </html>
            """;
    }

    private static string BuildWelcomeText(string name, string email, string password)
    {
        return $"""
            Olá, {name}!

            Sua conta na DhSchool foi criada com sucesso.

            Seus dados de acesso:
            Email: {email}
            Senha: {password}

            Recomendamos que você altere sua senha após o primeiro acesso.

            ---
            Este email foi enviado automaticamente pela DhSchool.
            """;
    }
}
