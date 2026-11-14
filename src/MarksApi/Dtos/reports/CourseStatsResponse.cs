namespace MarksApi.Dtos.Reports;

public sealed record CourseStatsResponse(
    int CourseId,
    string CourseCode,
    int Enrolled,
    int Graded,
    double? Average,
    int? Highest,
    int? Lowest,
    int Passed);