namespace TeacherApp.Contracts.Admin;

public sealed record CompletedLessonAdminRowResponse(
    Guid StudentId,
    string StudentEmail,
    string StudentFullName,
    Guid LessonId,
    string LessonTitle,
    Guid ModuleId,
    string ModuleTitle,
    DateTimeOffset CompletedAt);
