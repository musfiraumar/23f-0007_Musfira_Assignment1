using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MarksApi.SmokeTest;

// Exit codes: 0 = all checks passed, 1 = at least one check failed, 2 = could not run (config or API unreachable)
public static class SmokeTestRunner
{
    private const string Prefix = "SMOKE-";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static HttpClient _http = default!;
    private static int _passed;
    private static int _failed;

    private sealed record Reply(HttpStatusCode Status, JsonElement Body);

    public static async Task<int> RunAsync()
    {
        // Credentials come from configuration (user-secrets or environment), never from code
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets(typeof(SmokeTestRunner).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var baseUrl = config["SmokeTest:BaseUrl"] ?? "http://localhost:5062";
        var username = config["SmokeTest:Username"];
        var password = config["SmokeTest:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Console.Error.WriteLine(
                "SmokeTest:Username and SmokeTest:Password are not set. Use dotnet user-secrets (see README).");
            return 2;
        }

        _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(15) };
        string? token = null;

        try
        {
            Console.WriteLine($"Smoke test against {baseUrl}\n");

            var health = await SendAsync(HttpMethod.Get, "/health");
            CheckStatus("GET /health is 200", health, HttpStatusCode.OK);

            var bad = await SendAsync(HttpMethod.Post, "/api/auth/login",
                body: new { username, password = "definitely-wrong-password" });
            CheckStatus("Login with wrong password is 401", bad, HttpStatusCode.Unauthorized);

            var login = await SendAsync(HttpMethod.Post, "/api/auth/login", body: new { username, password });
            CheckStatus("Teacher login is 200", login, HttpStatusCode.OK);
            token = GetString(login.Body, "token");

            if (token is null)
            {
                Check("Login returned a token", false);
            }
            else
            {
                await SweepAsync(token); // remove leftovers from an earlier crashed run
                await ScenarioAsync(token);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Console.Error.WriteLine($"Cannot reach the API at {baseUrl}: {ex.Message}");
            Console.Error.WriteLine("Is the API running in another terminal?");
            return 2;
        }
        finally
        {
            if (token is not null)
            {
                try { await CleanupAsync(token); }
                catch (Exception ex) { Check("Cleanup finished", false, ex.Message); }
            }
        }

        Console.WriteLine($"\n{_passed} passed, {_failed} failed");
        return _failed == 0 ? 0 : 1;
    }

    private static async Task ScenarioAsync(string token)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var courseCode = Prefix + suffix[..4].ToUpperInvariant();     // 10 chars, the maximum allowed
        var regNo = Prefix + suffix[..8].ToUpperInvariant();
        var email = $"smoke-{suffix[..8]}@example.com";

        // Create course and student
        var course = await SendAsync(HttpMethod.Post, "/api/courses", token,
            new { code = courseCode, title = "SMOKE Test Course", creditHours = 3 });
        CheckStatus("Create course is 201", course, HttpStatusCode.Created);

        var student = await SendAsync(HttpMethod.Post, "/api/students", token,
            new { registrationNo = regNo, fullName = "SMOKE Test Student", email });
        CheckStatus("Create student is 201", student, HttpStatusCode.Created);

        var courseId = GetInt(course.Body, "id");
        var studentId = GetInt(student.Body, "id");
        if (courseId is null || studentId is null)
        {
            Check("Created ids are available", false);
            return;
        }

        // Duplicates
        var dupCourse = await SendAsync(HttpMethod.Post, "/api/courses", token,
            new { code = courseCode.ToLowerInvariant(), title = "Duplicate", creditHours = 3 });
        CheckStatus("Duplicate course code (different case) is 409", dupCourse, HttpStatusCode.Conflict);

        var dupStudent = await SendAsync(HttpMethod.Post, "/api/students", token,
            new { registrationNo = regNo, fullName = "Duplicate", email = $"other-{email}" });
        CheckStatus("Duplicate registration number is 409", dupStudent, HttpStatusCode.Conflict);

        // Enrollment
        var enroll = await SendAsync(HttpMethod.Post, $"/api/courses/{courseId}/enrollments", token,
            new { studentId });
        CheckStatus("Enroll student is 201", enroll, HttpStatusCode.Created);
        var enrollmentId = GetInt(enroll.Body, "id");

        var dupEnroll = await SendAsync(HttpMethod.Post, $"/api/courses/{courseId}/enrollments", token,
            new { studentId });
        CheckStatus("Duplicate enrollment is 409", dupEnroll, HttpStatusCode.Conflict);

        if (enrollmentId is null)
        {
            Check("Enrollment id is available", false);
            return;
        }

        // Marks
        var mark = await SendAsync(HttpMethod.Put, $"/api/enrollments/{enrollmentId}/mark", token,
            new { mark = 80 });
        CheckStatus("Set mark 80 is 200", mark, HttpStatusCode.OK);
        Check("Response contains mark 80", GetInt(mark.Body, "mark") == 80);

        var badMark = await SendAsync(HttpMethod.Put, $"/api/enrollments/{enrollmentId}/mark", token,
            new { mark = 101 });
        CheckStatus("Mark 101 is 400", badMark, HttpStatusCode.BadRequest);

        // Reports
        var stats = await SendAsync(HttpMethod.Get, $"/api/courses/{courseId}/stats", token);
        CheckStatus("Course stats is 200", stats, HttpStatusCode.OK);
        Check("Stats: enrolled 1, graded 1, passed 1",
            GetInt(stats.Body, "enrolled") == 1 && GetInt(stats.Body, "graded") == 1 && GetInt(stats.Body, "passed") == 1);
        Check("Stats: average, highest and lowest are 80",
            GetDouble(stats.Body, "average") == 80 && GetInt(stats.Body, "highest") == 80 && GetInt(stats.Body, "lowest") == 80);

        var transcript = await SendAsync(HttpMethod.Get, $"/api/students/{studentId}/transcript", token);
        CheckStatus("Transcript is 200", transcript, HttpStatusCode.OK);
        Check("Transcript weighted average is 80", GetDouble(transcript.Body, "weightedAverage") == 80);
        Check("Transcript lists the course with mark 80", TranscriptHas(transcript.Body, courseCode, 80));

        // Negatives
        var noToken = await SendAsync(HttpMethod.Get, "/api/students");
        CheckStatus("List students without token is 401", noToken, HttpStatusCode.Unauthorized);

        var unknown = await SendAsync(HttpMethod.Get, "/api/courses/999999/stats", token);
        CheckStatus("Stats for unknown course is 404", unknown, HttpStatusCode.NotFound);

        var blocked = await SendAsync(HttpMethod.Delete, $"/api/courses/{courseId}", token);
        CheckStatus("Delete course that has enrollments is 409", blocked, HttpStatusCode.Conflict);
    }

    // Deletes every SMOKE- course (with its enrollments) and then every SMOKE- student
    private static async Task SweepAsync(string token)
    {
        var courses = await SendAsync(HttpMethod.Get, "/api/courses?search=SMOKE-&pageSize=50", token);
        foreach (var c in Items(courses.Body))
        {
            var code = GetString(c, "code");
            var id = GetInt(c, "id");
            if (id is null || code is null || !code.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) continue;

            var enrollments = await SendAsync(HttpMethod.Get, $"/api/courses/{id}/enrollments?pageSize=50", token);
            foreach (var e in Items(enrollments.Body))
                await SendAsync(HttpMethod.Delete, $"/api/enrollments/{GetInt(e, "id")}", token);

            await SendAsync(HttpMethod.Delete, $"/api/courses/{id}", token);
        }

        var students = await SendAsync(HttpMethod.Get, "/api/students?search=SMOKE-&pageSize=50", token);
        foreach (var s in Items(students.Body))
        {
            var regNo = GetString(s, "registrationNo");
            var id = GetInt(s, "id");
            if (id is null || regNo is null || !regNo.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) continue;

            await SendAsync(HttpMethod.Delete, $"/api/students/{id}", token);
        }
    }

    private static async Task CleanupAsync(string token)
    {
        await SweepAsync(token);

        var courses = await SendAsync(HttpMethod.Get, "/api/courses?search=SMOKE-&pageSize=1", token);
        var students = await SendAsync(HttpMethod.Get, "/api/students?search=SMOKE-&pageSize=1", token);
        Check("Cleanup: no SMOKE- data left",
            GetInt(courses.Body, "totalCount") == 0 && GetInt(students.Body, "totalCount") == 0);
    }

    // ---------- helpers ----------

    private static async Task<Reply> SendAsync(HttpMethod method, string url, string? token = null, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: Json);

        using var response = await _http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();

        JsonElement element = default;
        if (!string.IsNullOrWhiteSpace(text))
        {
            try { element = JsonDocument.Parse(text).RootElement.Clone(); }
            catch (JsonException) { /* not JSON, leave as default */ }
        }
        return new Reply(response.StatusCode, element);
    }

    private static IEnumerable<JsonElement> Items(JsonElement body)
    {
        if (body.ValueKind == JsonValueKind.Object &&
            body.TryGetProperty("items", out var items) &&
            items.ValueKind == JsonValueKind.Array)
            return items.EnumerateArray().ToList();
        return new List<JsonElement>();
    }

    private static bool TranscriptHas(JsonElement body, string code, int mark)
    {
        if (body.ValueKind != JsonValueKind.Object ||
            !body.TryGetProperty("courses", out var courses) ||
            courses.ValueKind != JsonValueKind.Array) return false;

        return courses.EnumerateArray().Any(c =>
            string.Equals(GetString(c, "courseCode"), code, StringComparison.OrdinalIgnoreCase) &&
            GetInt(c, "mark") == mark);
    }

    private static int? GetInt(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var p) &&
        p.ValueKind == JsonValueKind.Number ? p.GetInt32() : null;

    private static double? GetDouble(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var p) &&
        p.ValueKind == JsonValueKind.Number ? p.GetDouble() : null;

    private static string? GetString(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var p) &&
        p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static void CheckStatus(string name, Reply reply, HttpStatusCode expected) =>
        Check(name, reply.Status == expected, $"expected {(int)expected}, got {(int)reply.Status}");

    private static void Check(string name, bool ok, string? detail = null)
    {
        if (ok) _passed++; else _failed++;
        Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
        Console.Write(ok ? "[PASS] " : "[FAIL] ");
        Console.ResetColor();
        Console.WriteLine(ok || detail is null ? name : $"{name} ({detail})");
    }
}