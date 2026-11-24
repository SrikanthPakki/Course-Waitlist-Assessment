using CourseWaitlist;
using Xunit;

namespace CourseWaitlist.Tests;

public class EnrollmentServiceTests
{
    private static EnrollmentService CreateService(int capacity = 2) =>
        new(new CourseOffering("DOTNET-101", capacity, "CS-101"));

    private static Learner Eligible(string id) => new(id, $"Learner {id}", ["CS-101"]);

    [Fact]
    public void EnrollsLearnerWhenCapacityIsAvailable()
    {
        var service = CreateService();
        var result = service.Enroll(Eligible("A"));

        Assert.Equal(EnrollmentStatus.Enrolled, result.Status);
        Assert.Contains("A", service.GetEnrolledLearnerIds());
        Assert.Equal(1, service.AvailableSeats);
    }

    [Fact]
    public void AddsEligibleLearnerToWaitlistWhenCourseIsFull()
    {
        var service = CreateService(1);
        service.Enroll(Eligible("A"));

        var result = service.Enroll(Eligible("B"));

        Assert.Equal(EnrollmentStatus.Waitlisted, result.Status);
        Assert.Equal(1, result.WaitlistPosition);
        Assert.Single(service.GetWaitlist());
    }

    [Fact]
    public void DuplicateEnrollmentDoesNotCreateDuplicateRecords()
    {
        var service = CreateService(1);
        service.Enroll(Eligible("A"));
        var duplicate = service.Enroll(Eligible("A"));

        Assert.Equal(EnrollmentStatus.AlreadyEnrolled, duplicate.Status);
        Assert.Empty(service.GetWaitlist());
        Assert.Single(service.GetEnrolledLearnerIds());
    }

    [Fact]
    public void DuplicateWaitlistRequestDoesNotCreateDuplicateRecords()
    {
        var service = CreateService(1);
        service.Enroll(Eligible("A"));
        service.Enroll(Eligible("B"));

        var duplicate = service.Enroll(Eligible("B"));

        Assert.Equal(EnrollmentStatus.AlreadyWaitlisted, duplicate.Status);
        Assert.Single(service.GetWaitlist());
    }

    [Fact]
    public void DropPromotesEarliestEligibleWaitlistedLearner()
    {
        var service = CreateService(1);
        service.Enroll(Eligible("A"));
        service.Enroll(Eligible("B"));
        service.Enroll(Eligible("C"));

        var result = service.Drop("A");

        Assert.Equal(DropStatus.Dropped, result.Status);
        Assert.Equal("B", result.PromotedLearnerId);
        Assert.Contains("B", service.GetEnrolledLearnerIds());
        Assert.DoesNotContain(service.GetWaitlist(), x => x.LearnerId == "B");
        Assert.Contains(service.GetWaitlist(), x => x.LearnerId == "C");
    }

    [Fact]
    public void PrerequisiteFailureDoesNotEnrollOrWaitlistLearner()
    {
        var service = CreateService(1);
        var result = service.Enroll(new Learner("X", "Learner X", ["MATH-101"]));

        Assert.Equal(EnrollmentStatus.PrerequisiteNotMet, result.Status);
        Assert.Empty(service.GetEnrolledLearnerIds());
        Assert.Empty(service.GetWaitlist());
    }

    [Fact]
    public void IneligibleLearnerAtQueueHeadIsSkippedAndRetained()
    {
        var service = CreateService(1);
        service.Enroll(Eligible("A"));
        service.Enroll(Eligible("B"));
        service.Enroll(Eligible("C"));

        Assert.True(service.UpdateCompletedCourses("B", ["MATH-101"]));
        var result = service.Drop("A");

        Assert.Equal("C", result.PromotedLearnerId);
        Assert.Contains(service.GetEnrolledLearnerIds(), x => x == "C");
        Assert.Contains(service.GetWaitlist(), x => x.LearnerId == "B");
        Assert.Equal(0, service.AvailableSeats);
    }

    [Fact]
    public void DropOfUnknownOrAlreadyDroppedLearnerIsSafe()
    {
        var service = CreateService(1);
        service.Enroll(Eligible("A"));

        Assert.Equal(DropStatus.NotEnrolled, service.Drop("UNKNOWN").Status);
        Assert.Equal(DropStatus.Dropped, service.Drop("A").Status);
        Assert.Equal(DropStatus.NotEnrolled, service.Drop("A").Status);
        Assert.Equal(1, service.AvailableSeats);
    }

    [Fact]
    public void InvalidRequestsReturnClearResults()
    {
        var service = CreateService();

        Assert.Equal(EnrollmentStatus.InvalidRequest, service.Enroll(null).Status);
        Assert.Equal(EnrollmentStatus.InvalidRequest, service.Enroll(new Learner("", "No ID")).Status);
        Assert.Equal(DropStatus.InvalidRequest, service.Drop(" ").Status);
    }

    [Fact]
    public void IfNoWaitlistedLearnerIsEligibleSeatRemainsOpen()
    {
        var service = CreateService(1);
        service.Enroll(Eligible("A"));
        service.Enroll(Eligible("B"));
        service.UpdateCompletedCourses("B", ["OTHER"]);

        var result = service.Drop("A");

        Assert.Null(result.PromotedLearnerId);
        Assert.Equal(1, service.AvailableSeats);
        Assert.Single(service.GetWaitlist());
    }

    [Fact]
    public async Task ConcurrentEnrollmentsNeverExceedCapacity()
    {
        const int capacity = 10;
        var service = CreateService(capacity);
        var tasks = Enumerable.Range(1, 100)
            .Select(i => Task.Run(() => service.Enroll(Eligible($"L{i:000}"))));

        await Task.WhenAll(tasks);

        Assert.Equal(capacity, service.GetEnrolledLearnerIds().Count);
        Assert.Equal(90, service.GetWaitlist().Count);
        Assert.Equal(0, service.AvailableSeats);
    }
}
