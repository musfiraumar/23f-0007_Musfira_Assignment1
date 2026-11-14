namespace MarksApi.Dtos.Reports;

public sealed record TranscriptCourseResponse(
    int CourseId,
    string CourseCode,
    string Title,
    int CreditHours,
    int? Mark,
    bool? Passed);