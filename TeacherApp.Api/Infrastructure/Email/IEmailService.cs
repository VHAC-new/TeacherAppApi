namespace TeacherApp.Api.Infrastructure.Email;

public interface IEmailService
{
    Task SendStudentWelcomeEmailAsync(string toEmail, string studentName, string provisionalPassword, CancellationToken ct = default);
}
