using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.LearningTranscript.DTOs;
using SkillsetsBackend.Application.LearningTranscript.Interfaces;
using SkillsetsBackend.Domain.Identity;

namespace SkillsetsBackend.Application.LearningTranscript.Commands.ImportLearningTranscriptBatch;

public class ImportLearningTranscriptBatchCommandHandler
{
    private readonly ILearningTranscriptImportService _importService;
    private readonly IPermissionService _permissionService;

    public ImportLearningTranscriptBatchCommandHandler(ILearningTranscriptImportService importService, IPermissionService permissionService)
    {
        _importService = importService;
        _permissionService = permissionService;
    }

    public async Task<LearningTranscriptImportResultDto> Handle(ImportLearningTranscriptBatchCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.LearningTranscript.Import, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to import learning transcript data.");
        }

        return await _importService.ImportAsync(command.FileContent, command.SourceFileName, caller.Email, cancellationToken);
    }
}
