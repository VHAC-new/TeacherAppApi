using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeacherApp.Api.Common;
using TeacherApp.Api.Data;
using TeacherApp.Api.Domain;
using TeacherApp.Contracts.Admin;

namespace TeacherApp.Api.Application.Admin.Students;

public sealed class StudentsAdminService(AppDbContext db) : IStudentsAdminService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<IReadOnlyList<AdminStudentPerformanceResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var students = await (
            from u in db.Users.AsNoTracking()
            join s in db.Students.AsNoTracking() on u.Id equals s.UserId
            where u.Role == Roles.Student
            orderby u.Email
            select new { u.Id, u.Email, s.FullName }
        ).ToListAsync(cancellationToken);

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
                    s.Id, s.Email, s.FullName, 0, 0, 0, null));
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
                s.Id, s.Email, s.FullName, progress, accuracy, totalAnswers, lastActivity));
        }

        return results;
    }

    public async Task<AdminStudentDetailsResponse?> GetDetailsAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var row = await (
            from u in db.Users.AsNoTracking()
            join s in db.Students.AsNoTracking() on u.Id equals s.UserId
            where u.Id == studentId && u.Role == Roles.Student
            select new
            {
                u.Id,
                u.Email,
                u.IsActive,
                s.FullName,
                s.Cpf,
                s.BirthDate,
                s.Phone,
                s.PostalCode,
                s.Address,
                s.Course,
            }
        ).FirstOrDefaultAsync(cancellationToken);

        if (row is null)
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
            row.Id,
            row.Email,
            row.IsActive,
            row.FullName,
            row.Cpf,
            row.BirthDate,
            row.Phone,
            row.PostalCode,
            row.Address,
            row.Course,
            totalExercises,
            accuracy,
            correctCount,
            incorrectCount,
            lastActivity);
    }

    public async Task<IReadOnlyList<AdminStudentAnswerResponse>> ListAnswersAsync(
        Guid studentId,
        Guid? moduleId,
        bool? isCorrect,
        int take,
        CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 500);

        var exists = await db.Users.AsNoTracking()
            .AnyAsync(u => u.Id == studentId && u.Role == Roles.Student, cancellationToken);
        if (!exists)
            return [];

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

    public async Task<CreateStudentResponse> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Full name is required.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var cpfDigits = StudentCpfNormalizer.ToDigits(request.Cpf);
        if (!StudentCpfNormalizer.IsValidBrazilianCpfDigits(cpfDigits))
            throw new ArgumentException("Invalid CPF.");

        if (string.IsNullOrWhiteSpace(request.Phone)
            || string.IsNullOrWhiteSpace(request.PostalCode)
            || string.IsNullOrWhiteSpace(request.Address)
            || string.IsNullOrWhiteSpace(request.Course))
        {
            throw new ArgumentException("Phone, postal code, address, and course are required.");
        }

        var emailTaken = await db.Users.AsNoTracking()
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
        if (emailTaken)
            throw new InvalidOperationException("Email is already registered.");

        var cpfTaken = await db.Students.AsNoTracking()
            .AnyAsync(s => s.Cpf == cpfDigits, cancellationToken);
        if (cpfTaken)
            throw new InvalidOperationException("CPF is already registered.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            Name = request.FullName.Trim(),
            Role = Roles.Student,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var provisionalPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        user.PasswordHash = _passwordHasher.HashPassword(user, provisionalPassword);

        var student = new Student
        {
            UserId = user.Id,
            FullName = request.FullName.Trim(),
            Cpf = cpfDigits,
            BirthDate = request.BirthDate,
            Phone = request.Phone.Trim(),
            PostalCode = request.PostalCode.Trim(),
            Address = request.Address.Trim(),
            Course = request.Course.Trim(),
        };

        db.Users.Add(user);
        db.Students.Add(student);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateStudentResponse(user.Id);
    }
}
