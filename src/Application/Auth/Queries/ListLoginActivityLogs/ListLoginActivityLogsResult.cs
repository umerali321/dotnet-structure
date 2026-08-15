using SkillsetsBackend.Application.Auth.DTOs;
using SkillsetsBackend.Shared.Common;

namespace SkillsetsBackend.Application.Auth.Queries.ListLoginActivityLogs;

public record ListLoginActivityLogsResult(PaginatedList<LoginActivityLogDto> Logs, LoginActivitySummaryDto Summary);
