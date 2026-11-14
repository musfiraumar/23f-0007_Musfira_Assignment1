using System.Net.Mail;
using MarksApi.Dtos.Students;

namespace MarksApi.Services;

public static class StudentRequestValidator
{
    public static Dictionary<string, string[]> Validate(StudentRequest r)
    {
        var errors = new Dictionary<string, string[]>();

        var regNo = r.RegistrationNo?.Trim();
        if (string.IsNullOrEmpty(regNo))
            errors["registrationNo"] = ["Registration number is required."];
        else if (regNo.Length > 20)
            errors["registrationNo"] = ["Registration number must be at most 20 characters."];

        var name = r.FullName?.Trim();
        if (string.IsNullOrEmpty(name))
            errors["fullName"] = ["Full name is required."];
        else if (name.Length > 100)
            errors["fullName"] = ["Full name must be at most 100 characters."];

        var email = r.Email?.Trim();
        if (string.IsNullOrEmpty(email))
            errors["email"] = ["Email is required."];
        else if (email.Length > 150)
            errors["email"] = ["Email must be at most 150 characters."];
        else if (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)
            errors["email"] = ["Email format is invalid."];

        return errors;
    }
}