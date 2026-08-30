using SkillsetsBackend.Application.Assignments;
using SkillsetsBackend.Application.Assignments.DTOs;

namespace SkillsetsBackend.UnitTests.Assignments;

/// <summary>The rule the customer signed off on: an assignment running Sept 1-30 keeps those dates
/// whatever the employee does, so someone who starts on Sept 10 is Late - the window does not move
/// to wherever they happened to begin.</summary>
public class AssignmentTimingTests
{
    private static readonly DateOnly AssignmentStart = new(2026, 9, 1);

    [Fact]
    public void NeverStarted_IsNotStarted()
    {
        Assert.Equal(AssignmentStartTiming.NotStarted, AssignmentTiming.Derive(null, AssignmentStart));
    }

    [Fact]
    public void StartedBeforeTheAssignmentBegan_IsEarly()
    {
        Assert.Equal(AssignmentStartTiming.Early, AssignmentTiming.Derive(new DateOnly(2026, 8, 28), AssignmentStart));
    }

    [Fact]
    public void StartedOnTheFirstDay_IsOnTime_NotEarly()
    {
        // The ordinary, expected case - labelling it "Early" would misreport almost everyone.
        Assert.Equal(AssignmentStartTiming.OnTime, AssignmentTiming.Derive(AssignmentStart, AssignmentStart));
    }

    [Fact]
    public void StartedAfterTheAssignmentBegan_IsLate()
    {
        // The exact example from the sign-off: Sept 1-30 assignment, started Sept 10.
        Assert.Equal(AssignmentStartTiming.Late, AssignmentTiming.Derive(new DateOnly(2026, 9, 10), AssignmentStart));
    }

    [Fact]
    public void StartedOneDayLate_IsLate()
    {
        Assert.Equal(AssignmentStartTiming.Late, AssignmentTiming.Derive(new DateOnly(2026, 9, 2), AssignmentStart));
    }

    [Fact]
    public void StartedAfterTheWindowClosed_IsStillLate()
    {
        Assert.Equal(AssignmentStartTiming.Late, AssignmentTiming.Derive(new DateOnly(2026, 12, 25), AssignmentStart));
    }

    [Fact]
    public void OneLateTitle_MakesTheWholeAssignmentLate()
    {
        // Starting one course on time doesn't excuse leaving another until after the window opened.
        var timings = new[] { AssignmentStartTiming.OnTime, AssignmentStartTiming.Late };
        Assert.Equal(
            AssignmentStartTiming.Late,
            AssignmentTiming.DeriveOverall(timings, new DateOnly(2026, 9, 1), AssignmentStart));
    }

    [Fact]
    public void NoLateTitles_UsesTheEarliestStart()
    {
        var timings = new[] { AssignmentStartTiming.Early, AssignmentStartTiming.NotStarted };
        Assert.Equal(
            AssignmentStartTiming.Early,
            AssignmentTiming.DeriveOverall(timings, new DateOnly(2026, 8, 20), AssignmentStart));
    }

    [Fact]
    public void NothingStartedAtAll_RollsUpAsNotStarted()
    {
        var timings = new[] { AssignmentStartTiming.NotStarted, AssignmentStartTiming.NotStarted };
        Assert.Equal(
            AssignmentStartTiming.NotStarted,
            AssignmentTiming.DeriveOverall(timings, null, AssignmentStart));
    }
}
