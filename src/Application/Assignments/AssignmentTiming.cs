using SkillsetsBackend.Application.Assignments.DTOs;

namespace SkillsetsBackend.Application.Assignments;

/// <summary>Works out whether an employee began their assigned training on time. Lives here rather
/// than inside the query service because it is a business rule, not a persistence concern - and
/// because it is the kind of rule worth covering with tests directly.</summary>
public static class AssignmentTiming
{
    /// <summary>
    /// Compares when they actually began against the assignment's OWN start date - never against
    /// the 30-day session that begins whenever they first click in. That was the previous behaviour
    /// and it meant someone who started three weeks late still looked on-track, which is exactly the
    /// problem this replaces: the dates are fixed when the assignment is created and do not move.
    /// </summary>
    public static AssignmentStartTiming Derive(DateOnly? startedOn, DateOnly assignmentStart)
    {
        if (startedOn is not { } started)
        {
            return AssignmentStartTiming.NotStarted;
        }

        if (started < assignmentStart)
        {
            return AssignmentStartTiming.Early;
        }

        // Starting on the very first day is precisely what was asked for - reporting that as Early
        // would mislabel the ordinary case.
        return started == assignmentStart ? AssignmentStartTiming.OnTime : AssignmentStartTiming.Late;
    }

    /// <summary>An employee's timing across a whole assignment. A single late title makes the whole
    /// thing late - starting one course on time does not excuse leaving another until after the
    /// window opened. Otherwise the earliest start decides.</summary>
    public static AssignmentStartTiming DeriveOverall(
        IReadOnlyCollection<AssignmentStartTiming> titleTimings, DateOnly? earliestStart, DateOnly assignmentStart)
    {
        return titleTimings.Contains(AssignmentStartTiming.Late)
            ? AssignmentStartTiming.Late
            : Derive(earliestStart, assignmentStart);
    }
}
