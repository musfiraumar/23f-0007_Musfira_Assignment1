namespace MarksApi.Data.Entities;
public class Student
{
    public int Id { get; set; }
    public string RegistrationNo { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public List<Enrollment> Enrollments { get; set; } = new();
}