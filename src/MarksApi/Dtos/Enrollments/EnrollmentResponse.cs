namespace MarksApi.Dtos.Enrollments;

public record EnrollmentResponse(int Id, int StudentId, int CourseId, int? Mark);