namespace MarksApi.Dtos.Reports;

public sealed record TranscriptResponse(
    int StudentId,
    string RegistrationNo,
    string FullName,
    IReadOnlyList<TranscriptCourseResponse> Courses,
    int GradedCreditHours,
    double? WeightedAverage);