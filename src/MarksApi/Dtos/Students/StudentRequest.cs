namespace MarksApi.Dtos.Students;

// Nullable so a missing field becomes a validation error, not a generic 400
public record StudentRequest(string? RegistrationNo, string? FullName, string? Email);