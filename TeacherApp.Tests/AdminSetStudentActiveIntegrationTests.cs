using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TeacherApp.Api.Common;
using TeacherApp.Api.Data;
using TeacherApp.Api.Domain;
using TeacherApp.Contracts.Admin;
using TeacherApp.Contracts.Auth;
using TeacherApp.Tests.Fixtures;

namespace TeacherApp.Tests;

public sealed class AdminSetStudentActiveIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public AdminSetStudentActiveIntegrationTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Deactivating_student_blocks_login_reactivating_restores_access()
    {
        var studentId = Guid.NewGuid();
        const string email = "active.toggle@test.local";
        const string password = "password123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = new PasswordHasher<User>();
            var user = new User
            {
                Id = studentId,
                Email = email,
                Name = "Toggle Student",
                Role = Roles.Student,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
            db.Students.Add(new Student
            {
                UserId = studentId,
                FullName = "Toggle Student",
                Cpf = "11144477735",
                BirthDate = new DateOnly(2000, 1, 15),
                Phone = "11999999999",
                PostalCode = "01310100",
                Address = "Rua Teste, 1",
                Course = "Inglês",
            });
            await db.SaveChangesAsync();
        }

        var admin = _factory.CreateAuthenticatedClient(Guid.NewGuid(), "admin-active@test.com", "Admin");
        var publicClient = _factory.CreateClient();

        var loginOk = await publicClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, loginOk.StatusCode);

        var deactivate = await admin.PutAsJsonAsync(
            $"/api/v1/admin/students/{studentId}/active",
            new SetStudentActiveRequest(false));
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);
        var inactiveDetails = await deactivate.Content.ReadFromJsonAsync<AdminStudentDetailsResponse>();
        Assert.NotNull(inactiveDetails);
        Assert.False(inactiveDetails.IsActive);

        var loginBlocked = await publicClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.Unauthorized, loginBlocked.StatusCode);

        var reactivate = await admin.PutAsJsonAsync(
            $"/api/v1/admin/students/{studentId}/active",
            new SetStudentActiveRequest(true));
        Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);
        var activeDetails = await reactivate.Content.ReadFromJsonAsync<AdminStudentDetailsResponse>();
        Assert.NotNull(activeDetails);
        Assert.True(activeDetails.IsActive);

        var loginRestored = await publicClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, loginRestored.StatusCode);
    }
}
