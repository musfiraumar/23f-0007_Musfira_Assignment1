namespace MarksApi.Dtos.Enrollments;

public record CourseEnrollmentResponse(
    int Id, int StudentId, string RegistrationNo, string FullName, int? Mark, DateTime EnrolledAtUtc);