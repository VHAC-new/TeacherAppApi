namespace TeacherApp.Contracts.Progress;

public sealed record LessonProgressResponse(
    Guid LessonId,
    bool IsCompleted,
    int TotalExercises,
    int CompletedExercises);
