namespace TeacherApp.Api.Domain;

public sealed class Student
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Address { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Cpf { get; set; } = "";
    public DateOnly? BirthDate { get; set; }
    public string Course { get; set; } = "";

    public User? User { get; set; }
}
