using System.Net;
using System.Net.Http.Json;
using TeacherApp.Contracts.Admin;
using TeacherApp.Tests.Fixtures;

namespace TeacherApp.Tests;

public sealed class AdminCreateStudentIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public AdminCreateStudentIntegrationTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_can_create_student_without_password_in_body()
    {
        var admin = _factory.CreateAuthenticatedClient(Guid.NewGuid(), "admin-create@test.com", "Admin");
        var email = $"new.student.{Guid.NewGuid():N}@test.local";
        var request = new CreateStudentRequest(
            Email: email,
            FullName: "Novo Aluno",
            Cpf: "39053344705",
            BirthDate: new DateOnly(1995, 6, 20),
            Phone: "(11) 98888-7766",
            PostalCode: "01310-100",
            Address: "Av. Paulista, 1000, São Paulo, SP",
            Course: "Inglês");

        var response = await admin.PostAsJsonAsync("/api/v1/admin/students", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateStudentResponse>();
        Assert.NotNull(body);

        var get = await admin.GetAsync($"/api/v1/admin/students/{body.UserId}");
        Assert.True(get.IsSuccessStatusCode);
        var details = await get.Content.ReadFromJsonAsync<AdminStudentDetailsResponse>();
        Assert.NotNull(details);
        Assert.Equal(email, details.Email);
        Assert.Equal("Novo Aluno", details.FullName);
        Assert.Equal("39053344705", details.Cpf);
    }

    [Fact]
    public async Task Student_cannot_create_student_via_admin_endpoint()
    {
        var student = _factory.CreateAuthenticatedClient(Guid.NewGuid(), "only.student@test.com", "Student");
        var request = new CreateStudentRequest(
            Email: "x@test.local",
            FullName: "X",
            Cpf: "11144477735",
            BirthDate: new DateOnly(2001, 1, 1),
            Phone: "11999999999",
            PostalCode: "00000-000",
            Address: "Addr",
            Course: "Espanhol");

        var response = await student.PostAsJsonAsync("/api/v1/admin/students", request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
