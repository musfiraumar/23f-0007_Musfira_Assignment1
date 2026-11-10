namespace MarksApi.Data.Entities;

public class Course
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int CreditHours { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<Enrollment> Enrollments { get; set; } = new();
}