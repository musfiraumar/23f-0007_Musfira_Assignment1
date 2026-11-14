using MarksApi.Dtos.Enrollments;

namespace MarksApi.Services;

public static class EnrollmentRequestValidator
{
    public static Dictionary<string, string[]> ValidateCreate(EnrollmentCreateRequest r)
    {
        var errors = new Dictionary<string, string[]>();
        if (r.StudentId is null || r.StudentId <= 0)
            errors["studentId"] = ["Student id is required."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateMark(MarkRequest r)
    {
        var errors = new Dictionary<string, string[]>();
        if (r.Mark is null)
            errors["mark"] = ["Mark is required."];
        else if (r.Mark < 0 || r.Mark > 100)
            errors["mark"] = ["Mark must be a whole number from 0 to 100."];
        return errors;
    }
}