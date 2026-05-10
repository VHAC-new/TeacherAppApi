namespace TeacherApp.Contracts.Admin;

public sealed record AdminStudentPerformanceResponse(
    Guid Id,
    string Email,
    string FullName,
    double ProgressPercent,
    double AccuracyPercent,
    int TotalAnswers,
    DateTimeOffset? LastActivity);
