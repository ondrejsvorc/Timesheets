using Microsoft.EntityFrameworkCore;
using Timesheets.Api.Domain;

namespace Timesheets.Api.Features.Projects;

internal static class ProjectArchiveGuard
{
    internal const string BlockMessage = "Archivovaný projekt nelze upravovat.";

    public static async Task<string?> BlockIfArchivedAsync(Guid projectId, AppDbContext dbContext, CancellationToken cancellationToken) =>
        await dbContext.Projects.AsNoTracking().AnyAsync(project => project.Id == projectId && project.ArchivedAt != null, cancellationToken)
            ? BlockMessage
            : null;

    public static async Task<string?> BlockIfContractArchivedAsync(Guid contractId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        Guid? projectId = await dbContext.Contracts
            .AsNoTracking()
            .Where(contract => contract.Id == contractId)
            .Select(contract => (Guid?)contract.ProjectId)
            .SingleOrDefaultAsync(cancellationToken);

        return projectId is null ? null : await BlockIfArchivedAsync(projectId.Value, dbContext, cancellationToken);
    }
}
