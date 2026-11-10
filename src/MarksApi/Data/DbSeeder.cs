using MarksApi.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MarksApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct)) return;   // already seeded

        var now = DateTime.UtcNow;

        var ali = new Student { RegistrationNo = "CS-2022-001", FullName = "Ali Raza", Email = "ali.raza@example.com", CreatedAtUtc = now };
        var sara = new Student { RegistrationNo = "CS-2022-002", FullName = "Sara Khan", Email = "sara.khan@example.com", CreatedAtUtc = now };

        var cs401 = new Course { Code = "CS401", Title = "Advanced Programming", CreditHours = 3, CreatedAtUtc = now };
        var ma201 = new Course { Code = "MA201", Title = "Linear Algebra", CreditHours = 2, CreatedAtUtc = now };
        var db301 = new Course { Code = "DB301", Title = "Database Systems", CreditHours = 4, CreatedAtUtc = now };

        var enrollments = new[]
        {
            new Enrollment { Student = ali,  Course = cs401, Mark = 80,   EnrolledAtUtc = now },
            new Enrollment { Student = ali,  Course = ma201, Mark = 60,   EnrolledAtUtc = now },
            new Enrollment { Student = ali,  Course = db301, Mark = null, EnrolledAtUtc = now },
            new Enrollment { Student = sara, Course = cs401, Mark = 70,   EnrolledAtUtc = now },
            new Enrollment { Student = sara, Course = ma201, Mark = 45,   EnrolledAtUtc = now },
            new Enrollment { Student = sara, Course = db301, Mark = 90,   EnrolledAtUtc = now },
        };

        var hasher = new PasswordHasher<User>();
        var users = new[]
        {
            MakeUser(hasher, "teacher1", "Teacher@123", "Teacher", null),
            MakeUser(hasher, "ali", "Student@123", "Student", ali),
            MakeUser(hasher, "sara", "Student@123", "Student", sara),
        };

        db.Enrollments.AddRange(enrollments);   // also adds the students and courses they point to
        db.Users.AddRange(users);
        await db.SaveChangesAsync(ct);
    }

    private static User MakeUser(PasswordHasher<User> hasher, string username, string password, string role, Student? student)
    {
        var user = new User { Username = username, Role = role, Student = student };
        user.PasswordHash = hasher.HashPassword(user, password);
        return user;
    }
}