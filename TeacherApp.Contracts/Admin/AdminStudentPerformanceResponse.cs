namespace TeacherApp.Contracts.Admin;

public sealed record AdminStudentPerformanceResponse(
    Guid Id,
    string Email,
    string? Name,
    double ProgressPercent,
    double AccuracyPercent,
    int TotalAnswers,
    DateTimeOffset? LastActivity);
