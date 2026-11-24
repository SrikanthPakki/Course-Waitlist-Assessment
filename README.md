# Agentic Proficient Engineering Assessment — Course Waitlist

A bounded .NET 8 implementation of enrollment, dropping, prerequisite checks, and FIFO waitlist promotion for one course offering.

## Prerequisites
- .NET 8 SDK

## Run the sample
From the repository root:

```bash
dotnet restore
dotnet run --project src/CourseWaitlist/CourseWaitlist.csproj
```

The sample creates a course with capacity 2, enrolls two learners, waitlists a third, and demonstrates promotion after a drop.

## Run automated tests

```bash
dotnet test CourseWaitlistAssessment.sln
```

## Build

```bash
dotnet build CourseWaitlistAssessment.sln --configuration Release
```

## Deliverables
- Runnable service and CLI sample: `src/CourseWaitlist/`
- Focused xUnit tests: `tests/CourseWaitlist.Tests/`
- Design specification: `docs/DESIGN.md`
- AI prompt/use record and verification note: `docs/AI-USE-RECORD.md`

## Important verification note
The project was authored against .NET 8, but the generation environment did not include the .NET SDK. No build or test pass is claimed. Execute the commands above locally and inspect the results before submission.
