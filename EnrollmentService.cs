namespace CourseWaitlist;

/// <summary>
/// Thread-safe enrollment and FIFO waitlist service for one course offering.
/// All state transitions are protected by one lock because this bounded
/// implementation models a single offering and an in-memory store.
/// </summary>
public sealed class EnrollmentService
{
    private readonly object _gate = new();
    private readonly CourseOffering _offering;
    private readonly Dictionary<string, Learner> _learners = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _enrolled = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<WaitlistEntry> _waitlist = [];
    private long _nextSequence = 1;

    public EnrollmentService(CourseOffering offering)
    {
        ArgumentNullException.ThrowIfNull(offering);
        if (string.IsNullOrWhiteSpace(offering.CourseId))
            throw new ArgumentException("Course ID is required.", nameof(offering));
        if (offering.Capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(offering), "Capacity must be greater than zero.");

        _offering = offering;
    }

    public EnrollmentResult Enroll(Learner? learner)
    {
        if (learner is null || string.IsNullOrWhiteSpace(learner.LearnerId))
            return new(EnrollmentStatus.InvalidRequest, "A valid learner and learner ID are required.");
        if (string.IsNullOrWhiteSpace(learner.Name))
            return new(EnrollmentStatus.InvalidRequest, "Learner name is required.");

        lock (_gate)
        {
            var id = learner.LearnerId.Trim();

            if (_enrolled.Contains(id))
                return new(EnrollmentStatus.AlreadyEnrolled, "Learner is already enrolled.");
            if (_waitlist.Any(x => string.Equals(x.LearnerId, id, StringComparison.OrdinalIgnoreCase)))
                return new(EnrollmentStatus.AlreadyWaitlisted, "Learner is already on the waitlist.");

            // Keep an internal snapshot so callers cannot mutate tracked eligibility
            // outside the service lock.
            var trackedLearner = new Learner(id, learner.Name, learner.CompletedCourseIds);
            _learners[id] = trackedLearner;

            if (!IsEligible(trackedLearner))
                return new(EnrollmentStatus.PrerequisiteNotMet, "Required prerequisite has not been completed.");

            if (_enrolled.Count < _offering.Capacity)
            {
                _enrolled.Add(id);
                return new(EnrollmentStatus.Enrolled, "Enrollment successful.");
            }

            var entry = new WaitlistEntry(id, _nextSequence++);
            _waitlist.Add(entry);
            var position = _waitlist.OrderBy(x => x.Sequence).ToList()
                .FindIndex(x => x.Sequence == entry.Sequence) + 1;

            return new(EnrollmentStatus.Waitlisted, "Course is full; learner added to the waitlist.", position);
        }
    }

    public DropResult Drop(string? learnerId)
    {
        if (string.IsNullOrWhiteSpace(learnerId))
            return new(DropStatus.InvalidRequest, "A valid learner ID is required.");

        lock (_gate)
        {
            var id = learnerId.Trim();
            if (!_enrolled.Remove(id))
                return new(DropStatus.NotEnrolled, "Learner is not currently enrolled.");

            var promoted = PromoteEarliestEligible();
            return new(
                DropStatus.Dropped,
                promoted is null
                    ? "Enrollment dropped; no eligible waitlisted learner was available."
                    : $"Enrollment dropped; learner {promoted} was promoted.",
                promoted);
        }
    }

    /// <summary>
    /// Updates a learner's completed courses. This makes eligibility changes
    /// explicit and allows the promotion policy to handle a now-ineligible
    /// learner already waiting in the queue.
    /// </summary>
    public bool UpdateCompletedCourses(string? learnerId, IEnumerable<string>? completedCourseIds)
    {
        if (string.IsNullOrWhiteSpace(learnerId) || completedCourseIds is null)
            return false;

        lock (_gate)
        {
            if (!_learners.TryGetValue(learnerId.Trim(), out var learner))
                return false;

            learner.CompletedCourseIds.Clear();
            learner.CompletedCourseIds.UnionWith(completedCourseIds.Where(x => !string.IsNullOrWhiteSpace(x)));
            return true;
        }
    }

    public IReadOnlyCollection<string> GetEnrolledLearnerIds()
    {
        lock (_gate)
            return _enrolled.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public IReadOnlyList<WaitlistEntry> GetWaitlist()
    {
        lock (_gate)
            return _waitlist.OrderBy(x => x.Sequence).ToArray();
    }

    public int AvailableSeats
    {
        get
        {
            lock (_gate)
                return _offering.Capacity - _enrolled.Count;
        }
    }

    private bool IsEligible(Learner learner) =>
        _offering.RequiredPrerequisiteCourseId is null ||
        learner.CompletedCourseIds.Contains(_offering.RequiredPrerequisiteCourseId);

    private string? PromoteEarliestEligible()
    {
        // Ineligible entries are skipped but retained in their original FIFO position.
        var candidate = _waitlist
            .OrderBy(x => x.Sequence)
            .FirstOrDefault(x =>
                _learners.TryGetValue(x.LearnerId, out var learner) && IsEligible(learner));

        if (candidate is null)
            return null;

        _waitlist.Remove(candidate);
        _enrolled.Add(candidate.LearnerId);
        return candidate.LearnerId;
    }
}
