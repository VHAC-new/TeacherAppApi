using Microsoft.EntityFrameworkCore;
using TeacherApp.Api.Common;
using TeacherApp.Api.Data;
using TeacherApp.Contracts.Admin;

namespace TeacherApp.Api.Application.Admin.Students;

public sealed class StudentsAdminService(AppDbContext db) : IStudentsAdminService
{
    public async Task<IReadOnlyList<AdminStudentPerformanceResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var students = await db.Users.AsNoTracking()
            .Where(u => u.Role == Roles.Student)
            .OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Email, u.Name })
            .ToListAsync(cancellationToken);

        var totalExercises = await db.Exercises.CountAsync(cancellationToken);

        var attempts = await db.ExerciseAttempts.AsNoTracking()
            .Where(a => a.ExerciseId != null)
            .Select(a => new { a.UserId, ExerciseId = a.ExerciseId!.Value, a.IsCorrect, a.AttemptedAt })
            .ToListAsync(cancellationToken);

        var attemptsByUser = attempts.GroupBy(a => a.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<AdminStudentPerformanceResponse>(students.Count);

        foreach (var s in students)
        {
            if (!attemptsByUser.TryGetValue(s.Id, out var userAttempts))
            {
                results.Add(new AdminStudentPerformanceResponse(
                    s.Id, s.Email, s.Name, 0, 0, 0, null));
                continue;
            }

            var totalAnswers = userAttempts.Count;
            var correctAnswers = userAttempts.Count(a => a.IsCorrect);
            var accuracy = totalAnswers > 0 ? Math.Round(100.0 * correctAnswers / totalAnswers, 1) : 0;

            var distinctCorrectExercises = userAttempts
                .Where(a => a.IsCorrect)
                .Select(a => a.ExerciseId)
                .Distinct()
                .Count();
            var progress = totalExercises > 0
                ? Math.Round(100.0 * distinctCorrectExercises / totalExercises, 1)
                : 0;

            var lastActivity = userAttempts.Max(a => a.AttemptedAt);

            results.Add(new AdminStudentPerformanceResponse(
                s.Id, s.Email, s.Name, progress, accuracy, totalAnswers, lastActivity));
        }

        return results;
    }

    public async Task<AdminStudentDetailsResponse?> GetDetailsAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var student = await db.Users.AsNoTracking()
            .Where(u => u.Id == studentId && u.Role == Roles.Student)
            .Select(u => new { u.Id, u.Email, u.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (student is null)
            return null;

        var attempts = await db.ExerciseAttempts.AsNoTracking()
            .Where(a => a.UserId == studentId && a.ExerciseId != null)
            .Select(a => new { a.IsCorrect, a.AttemptedAt })
            .ToListAsync(cancellationToken);

        var totalExercises = attempts.Count;
        var correctCount = attempts.Count(a => a.IsCorrect);
        var incorrectCount = totalExercises - correctCount;
        var accuracy = totalExercises > 0 ? Math.Round(100.0 * correctCount / totalExercises, 1) : 0;
        var lastActivity = attempts.Count > 0 ? attempts.Max(a => a.AttemptedAt) : (DateTimeOffset?)null;

        return new AdminStudentDetailsResponse(
            student.Id, student.Email, student.Name,
            totalExercises, accuracy, correctCount, incorrectCount, lastActivity);
    }

    public async Task<IReadOnlyList<AdminStudentAnswerResponse>> ListAnswersAsync(
        Guid studentId,
        Guid? moduleId,
        bool? isCorrect,
        int take,
        CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 500);

        var query = from a in db.ExerciseAttempts.AsNoTracking()
                    join e in db.Exercises.AsNoTracking() on a.ExerciseId equals e.Id
                    join l in db.Lessons.AsNoTracking() on e.LessonId equals l.Id
                    join m in db.Modules.AsNoTracking() on l.ModuleId equals m.Id
                    where a.UserId == studentId && a.ExerciseId != null
                    select new { a, e, l, m };

        if (moduleId.HasValue)
            query = query.Where(x => x.m.Id == moduleId.Value);

        if (isCorrect.HasValue)
            query = query.Where(x => x.a.IsCorrect == isCorrect.Value);

        var rows = await query
            .OrderByDescending(x => x.a.AttemptedAt)
            .Take(take)
            .Select(x => new AdminStudentAnswerResponse(
                x.a.Id,
                x.e.Id,
                x.e.Prompt,
                x.a.SubmittedAnswer,
                x.e.ExpectedAnswer,
                x.a.IsCorrect,
                x.m.Id,
                x.m.Title,
                x.l.Id,
                x.l.Title,
                x.a.AttemptedAt))
            .ToListAsync(cancellationToken);

        return rows;
    }
}
