using TeacherApp.Contracts.Admin;

namespace TeacherApp.Api.Application.Admin.Students;

public interface IStudentsAdminService
{
    Task<IReadOnlyList<AdminStudentPerformanceResponse>> ListAsync(CancellationToken cancellationToken);

    Task<AdminStudentDetailsResponse?> GetDetailsAsync(Guid studentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminStudentAnswerResponse>> ListAnswersAsync(
        Guid studentId,
        Guid? moduleId,
        bool? isCorrect,
        int take,
        CancellationToken cancellationToken);

    Task<CreateStudentResponse> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken);

    Task<AdminStudentDetailsResponse?> SetActiveAsync(Guid studentId, bool isActive, CancellationToken cancellationToken);
}
