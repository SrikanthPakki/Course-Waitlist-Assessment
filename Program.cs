using CourseWaitlist;

var offering = new CourseOffering("DOTNET-101", Capacity: 2, RequiredPrerequisiteCourseId: "CS-101");
var service = new EnrollmentService(offering);

var learners = new[]
{
    new Learner("L001", "Asha", ["CS-101"]),
    new Learner("L002", "Ravi", ["CS-101"]),
    new Learner("L003", "Maya", ["CS-101"]),
};

foreach (var learner in learners)
{
    var result = service.Enroll(learner);
    Console.WriteLine($"{learner.LearnerId}: {result.Status} - {result.Message}");
}

var drop = service.Drop("L001");
Console.WriteLine($"Drop: {drop.Message}");
Console.WriteLine($"Enrolled: {string.Join(", ", service.GetEnrolledLearnerIds())}");
Console.WriteLine($"Waitlist count: {service.GetWaitlist().Count}");
