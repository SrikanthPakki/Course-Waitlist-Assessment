# AI Prompt and Use Record

## Activity 1 — Task decomposition and design
**Prompt used (summary):** Review the assessment requirements for a single-course enrollment, drop, prerequisite validation, FIFO waitlist promotion, tests, design specification, and AI-use record. Propose a bounded .NET implementation and identify invariants and edge cases.

**AI response summary:** Recommended a single `EnrollmentService`, an in-memory store, explicit enrollment/drop result types, sequence-based FIFO ordering, a documented policy for ineligible waitlisted learners, and focused unit tests.

**Disposition:** Accepted the bounded service approach and explicit result types. The eligibility policy was made explicit as: skip an ineligible learner during promotion while retaining their waitlist position. The policy is tested.

**Files/decisions informed:** `Models.cs`, `EnrollmentService.cs`, `DESIGN.md`, and the test plan.

## Activity 2 — Implementation and test design
**Prompt used (summary):** Generate a thread-safe C# service for one course offering with enrollment, duplicate protection, prerequisite validation, drop, FIFO promotion, eligibility updates, and xUnit tests including concurrency.

**AI response summary:** Produced a lock-protected service and tests for available/full capacity, duplicate requests, FIFO promotion, prerequisite failure, invalid/repeated drops, ineligible queue-head handling, no eligible candidate, and concurrent enrollment.

**Disposition:** Used as an implementation draft. The code and test scenarios were reviewed against each acceptance criterion; naming and documentation were aligned to the assessment. The implementation uses a single lock because only one offering is in scope.

**Files informed:** `EnrollmentService.cs`, `Models.cs`, `Program.cs`, and `EnrollmentServiceTests.cs`.

## Verification evidence and honesty note
- The test suite is provided in `tests/CourseWaitlist.Tests/EnrollmentServiceTests.cs`.
- Intended verification commands are `dotnet restore`, `dotnet build`, and `dotnet test`.
- This environment did not have the .NET SDK/compiler installed, so the commands could not be executed here. Therefore, no passing test result is claimed. Run the commands in a .NET 8 environment before submission and record the actual output.
- Manual review checked the state transitions and expected outcomes represented by the tests. This is not a substitute for executing the tests.
- No credentials, secrets, or personal sensitive data were included in prompts.
