namespace TeacherApp.Contracts.Admin;

public sealed record AdminStudentDetailsResponse(
    Guid Id,
    string Email,
    string? Name,
    int TotalExercises,
    double AccuracyPercent,
    int CorrectCount,
    int IncorrectCount,
    DateTimeOffset? LastActivity);
