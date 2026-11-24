# Design Specification — Course Waitlist

## 1. Scope and service boundary
This implementation covers one course offering only. `EnrollmentService` owns enrollment, drop, eligibility checks, waitlist ordering, and promotion. `Program.cs` is a small runnable demonstration. There is no HTTP API or frontend because neither is required by the supplied assessment.

## 2. Data model
- `CourseOffering`: course ID, positive capacity, and optional required prerequisite course ID.
- `Learner`: learner ID, display name, and completed course IDs.
- Enrollment state: a set of enrolled learner IDs.
- Waitlist state: entries containing learner ID and a monotonically increasing sequence number.
- Learner profiles are retained in memory so eligibility can be re-evaluated at promotion time.

## 3. Assumptions and policies
1. One learner may have at most one active state for this offering: enrolled or waitlisted.
2. A prerequisite is satisfied when the learner's completed-course set contains the offering's required prerequisite ID. If no prerequisite is configured, all learners satisfy this rule.
3. A learner who is ineligible at the time of an enrollment request is rejected and is not added to the waitlist.
4. If a learner already on the waitlist later becomes ineligible, promotion skips that learner but retains their original queue position. The first eligible learner after them is promoted.
5. Eligibility changes are represented through `UpdateCompletedCourses`; this is included to make the skip/retain rule testable and explicit.
6. A drop is valid only for a currently enrolled learner. Dropping a waitlisted or unknown learner returns `NotEnrolled`.
7. If no waitlisted learner is eligible when a seat opens, the seat remains open and the ineligible waitlist entries remain queued.

## 4. Invariants
- `0 <= enrolled count <= offering capacity`.
- An ID cannot appear in both enrolled and waitlisted state.
- An ID can appear no more than once in the waitlist.
- Waitlist sequence values are unique and strictly increasing on insertion.
- Only eligible learners are enrolled or promoted.
- A successful drop removes exactly one enrolled ID before promotion is attempted.

## 5. Operations
### Enroll
Validate request, reject duplicates, check prerequisite, then either enroll immediately if a seat exists or append one waitlist entry with the next sequence number.

### Drop
Validate ID and remove it from enrolled state. Scan the waitlist in ascending sequence order and promote the first currently eligible learner. Ineligible entries are skipped and retained.

### Update eligibility
Replace the tracked learner's completed-course set. Eligibility is re-evaluated on the next promotion attempt.

## 6. Concurrency and persistence
All state-changing operations and state reads use a single private lock. This makes each operation atomic for this single-offering, in-memory prototype and prevents concurrent enrollments from exceeding capacity. A production multi-instance service would require a transactional database operation or equivalent distributed concurrency control (for example, a row lock/serializable transaction or atomic capacity reservation). The in-memory store is not durable and is not shared across processes.

## 7. Tradeoffs and limitations
- One global lock favors correctness and simplicity over throughput.
- State is lost when the process exits.
- There is no authentication, authorization, API transport, persistence, observability, or multi-offering support.
- Prerequisite evaluation is a simple completed-course ID check, not a full academic rules engine.
- The waitlist is a list sorted by sequence for promotion; this is adequate for the bounded exercise but could be replaced with a priority queue plus eligibility index for larger workloads.
