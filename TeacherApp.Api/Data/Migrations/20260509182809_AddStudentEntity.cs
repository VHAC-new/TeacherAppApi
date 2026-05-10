using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Student",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Cpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Course = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Student_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Student_Cpf",
                table: "Student",
                column: "Cpf",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "Student" ("UserId", "FullName", "Address", "PostalCode", "Phone", "Cpf", "BirthDate", "Course")
                SELECT u."Id",
                       COALESCE(NULLIF(BTRIM(u."Name"), ''), SPLIT_PART(LOWER(u."Email"), '@', 1)),
                       '',
                       '',
                       '',
                       'L' || SUBSTRING(MD5(u."Id"::text) FROM 1 FOR 10),
                       NULL,
                       ''
                FROM "User" u
                WHERE u."Role" = 'Student'
                  AND NOT EXISTS (SELECT 1 FROM "Student" s WHERE s."UserId" = u."Id");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Student");
        }
    }
}
