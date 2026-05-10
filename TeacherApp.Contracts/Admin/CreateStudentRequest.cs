namespace TeacherApp.Contracts.Admin;

public sealed record CreateStudentRequest(
    string Email,
    string FullName,
    string Cpf,
    DateOnly BirthDate,
    string Phone,
    string PostalCode,
    string Address,
    string Course);
