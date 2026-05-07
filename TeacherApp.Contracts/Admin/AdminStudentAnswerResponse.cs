namespace TeacherApp.Contracts.Admin;

public sealed record AdminStudentAnswerResponse(
    Guid AttemptId,
    Guid ExerciseId,
    string ExercisePrompt,
    string SubmittedAnswer,
    string ExpectedAnswer,
    bool IsCorrect,
    Guid ModuleId,
    string ModuleTitle,
    Guid LessonId,
    string LessonTitle,
    DateTimeOffset AttemptedAt);
