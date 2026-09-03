using Microsoft.EntityFrameworkCore;
using SkillsetsBackend.Application.LearningTranscript.DTOs;
using SkillsetsBackend.Application.LearningTranscript.Interfaces;
using SkillsetsBackend.Infrastructure.Persistence;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Infrastructure.LearningTranscript;

/// <summary>Backed by dbo.sp_ListLearningTranscript / dbo.sp_LearningTranscriptStats (see
/// Persistence/Migrations) rather than inline LINQ, per the same team convention CompanyQueryService
/// follows: the multi-table join + scoping + pagination logic lives in one stored procedure.</summary>
public class LearningTranscriptQueryService : ILearningTranscriptQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public LearningTranscriptQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedList<LearningTranscriptListItemDto>> ListAsync(LearningTranscriptQueryOptions options, CancellationToken cancellationToken = default)
    {
        // Distinguish "no restriction" (null) from "restricted to zero companies" (empty
        // collection) - the latter must return nothing without a round-trip, matching
        // CompanyQueryService.ListAsync's identical guard.
        if (options.RestrictToCompanyIds is { Count: 0 })
        {
            return new PaginatedList<LearningTranscriptListItemDto>([], 0, options.Page, options.PageSize);
        }

        var restrictParam = options.RestrictToCompanyIds is null ? null : string.Join(",", options.RestrictToCompanyIds);

        var rows = await _dbContext.Database
            .SqlQuery<Row>(
                $"""
                EXEC dbo.sp_ListLearningTranscript
                    @RestrictToCompanyIds={restrictParam},
                    @RestrictToUserId={options.RestrictToUserId},
                    @RestrictToManagerId={options.RestrictToManagerId},
                    @Search={options.Search},
                    @AssetId={options.AssetId},
                    @CompletionStatus={options.CompletionStatus},
                    @DateFrom={options.DateFrom},
                    @DateTo={options.DateTo},
                    @Page={options.Page},
                    @PageSize={options.PageSize}
                """)
            .ToListAsync(cancellationToken);

        var totalCount = rows.Count > 0 ? rows[0].TotalCount : 0;
        var items = rows.Select(r => new LearningTranscriptListItemDto(
            r.LearningTranscriptActivityId, r.UserId, r.EmployeeFirstName, r.EmployeeLastName, r.EmployeeEmail,
            r.StudentType,
            r.CompanyId, r.CompanyName, r.ManagerId, r.ManagerFirstName, r.ManagerLastName,
            r.UserStatus, r.GroupName, r.GroupOrgCode, r.GroupPath, r.TotalSessions,
            r.AssetId, r.AssetTitle, r.AssetType, r.AssetSubType,
            r.EnrollmentDate, r.FirstAccessDate, r.LastAccessDate, r.CompletionDate, r.CompletionStatus,
            r.HighScore, r.CurrentScore, r.PreTestScore, r.MaxTestAttempts, r.ActualTestAttempts,
            r.ExpectedDurationMinutes, r.ActualDurationMinutes, r.TimesAccessed, r.TimesDownloaded, r.TimesRestarted,
            r.AbsoluteFirstAccessDate, r.AbsoluteLastAccessDate, r.AbsoluteTimesAccessed,
            r.AbsoluteHighScore, r.AbsoluteLastScore, r.AbsoluteActualDurationMinutes,
            r.LastSkillportLoginDate, r.SkillportRegistrationDate))
            .ToList();

        return new PaginatedList<LearningTranscriptListItemDto>(items, totalCount, options.Page, options.PageSize);
    }

    public async Task<LearningTranscriptStatsDto> GetStatsAsync(LearningTranscriptQueryOptions options, CancellationToken cancellationToken = default)
    {
        if (options.RestrictToCompanyIds is { Count: 0 })
        {
            return new LearningTranscriptStatsDto(0, 0, 0, 0, 0, 0m, 0m);
        }

        var restrictParam = options.RestrictToCompanyIds is null ? null : string.Join(",", options.RestrictToCompanyIds);

        var rows = await _dbContext.Database
            .SqlQuery<StatsRow>(
                $"""
                EXEC dbo.sp_LearningTranscriptStats
                    @RestrictToCompanyIds={restrictParam},
                    @RestrictToUserId={options.RestrictToUserId},
                    @RestrictToManagerId={options.RestrictToManagerId},
                    @DateFrom={options.DateFrom},
                    @DateTo={options.DateTo}
                """)
            .ToListAsync(cancellationToken);

        var row = rows.Count > 0 ? rows[0] : new StatsRow(0, 0, 0, 0, 0, 0m, 0m);
        return new LearningTranscriptStatsDto(
            row.PeopleWithActivity, row.DistinctCoursesTaken, row.TotalCompletions, row.TotalInProgress,
            row.TotalActivityRows, row.CompletionRatePercent, row.AvgSessionsPerEmployee);
    }

    private sealed record Row(
        long LearningTranscriptActivityId, int UserId, string? EmployeeFirstName, string? EmployeeLastName, string? EmployeeEmail,
        string? StudentType,
        int? CompanyId, string? CompanyName, int? ManagerId, string? ManagerFirstName, string? ManagerLastName,
        string? UserStatus, string? GroupName, string? GroupOrgCode, string? GroupPath, int TotalSessions,
        string AssetId, string AssetTitle, string? AssetType, string? AssetSubType,
        DateOnly? EnrollmentDate, DateOnly? FirstAccessDate, DateOnly? LastAccessDate, DateOnly? CompletionDate, string? CompletionStatus,
        decimal? HighScore, decimal? CurrentScore, decimal? PreTestScore, int? MaxTestAttempts, int? ActualTestAttempts,
        int? ExpectedDurationMinutes, int? ActualDurationMinutes, int? TimesAccessed, int? TimesDownloaded, int? TimesRestarted,
        DateOnly? AbsoluteFirstAccessDate, DateOnly? AbsoluteLastAccessDate, int? AbsoluteTimesAccessed,
        decimal? AbsoluteHighScore, decimal? AbsoluteLastScore, int? AbsoluteActualDurationMinutes,
        DateOnly? LastSkillportLoginDate, DateOnly? SkillportRegistrationDate, int TotalCount);

    private sealed record StatsRow(
        int PeopleWithActivity, int DistinctCoursesTaken, int TotalCompletions, int TotalInProgress,
        int TotalActivityRows, decimal CompletionRatePercent, decimal AvgSessionsPerEmployee);
}
