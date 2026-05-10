namespace TeacherApp.Contracts.Admin;

public sealed record AdminStudentDetailsResponse(
    Guid Id,
    string Email,
    bool IsActive,
    string FullName,
    string Cpf,
    DateOnly? BirthDate,
    string Phone,
    string PostalCode,
    string Address,
    string Course,
    int TotalExercises,
    double AccuracyPercent,
    int CorrectCount,
    int IncorrectCount,
    DateTimeOffset? LastActivity);
