namespace MarksApi.Dtos.Courses;

// Nullable so a missing field becomes a validation error
public record CourseRequest(string? Code, string? Title, int? CreditHours);