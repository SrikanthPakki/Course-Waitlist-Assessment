namespace CourseWaitlist;

public sealed record CourseOffering(
    string CourseId,
    int Capacity,
    string? RequiredPrerequisiteCourseId = null);

public sealed class Learner
{
    public string LearnerId { get; }
    public string Name { get; }
    public HashSet<string> CompletedCourseIds { get; }

    public Learner(string learnerId, string name, IEnumerable<string>? completedCourseIds = null)
    {
        LearnerId = learnerId;
        Name = name;
        CompletedCourseIds = new HashSet<string>(
            completedCourseIds ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
    }
}

public enum EnrollmentStatus
{
    Enrolled,
    Waitlisted,
    AlreadyEnrolled,
    AlreadyWaitlisted,
    PrerequisiteNotMet,
    InvalidRequest
}

public sealed record EnrollmentResult(
    EnrollmentStatus Status,
    string Message,
    int? WaitlistPosition = null);

public enum DropStatus
{
    Dropped,
    NotEnrolled,
    InvalidRequest
}

public sealed record DropResult(
    DropStatus Status,
    string Message,
    string? PromotedLearnerId = null);

public sealed record WaitlistEntry(string LearnerId, long Sequence);
