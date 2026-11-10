namespace MarksApi.Data.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string Role { get; set; } = default!;
    public int? StudentId { get; set; }
    public Student? Student { get; set; }
}