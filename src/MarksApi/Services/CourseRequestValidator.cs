using MarksApi.Dtos.Courses;

namespace MarksApi.Services;

public static class CourseRequestValidator
{
    public static Dictionary<string, string[]> Validate(CourseRequest r)
    {
        var errors = new Dictionary<string, string[]>();

        var code = r.Code?.Trim();
        if (string.IsNullOrEmpty(code))
            errors["code"] = ["Course code is required."];
        else if (code.Length > 10)
            errors["code"] = ["Course code must be at most 10 characters."];

        var title = r.Title?.Trim();
        if (string.IsNullOrEmpty(title))
            errors["title"] = ["Title is required."];
        else if (title.Length > 150)
            errors["title"] = ["Title must be at most 150 characters."];

        if (r.CreditHours is null)
            errors["creditHours"] = ["Credit hours are required."];
        else if (r.CreditHours < 1 || r.CreditHours > 4)
            errors["creditHours"] = ["Credit hours must be between 1 and 4."];

        return errors;
    }
}