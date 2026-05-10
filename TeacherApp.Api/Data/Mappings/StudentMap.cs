using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeacherApp.Api.Domain;

namespace TeacherApp.Api.Data.Mappings;

public sealed class StudentMap : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Student");

        builder.HasKey(x => x.UserId);

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.PostalCode)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(x => x.Phone)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.Cpf)
            .IsRequired()
            .HasMaxLength(14);

        builder.HasIndex(x => x.Cpf)
            .IsUnique();

        builder.Property(x => x.BirthDate);

        builder.Property(x => x.Course)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<Student>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
