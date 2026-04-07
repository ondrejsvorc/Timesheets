using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Timesheets.Api.Common.Extensions;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Notifications;

namespace Timesheets.Api.Timesheets.Endpoints;

public sealed class UpdateTimesheetStatus : IEndpoint
{
    private static readonly Guid DraftStatusId = Guid.Parse("00000000-0000-0000-0000-000000000020");
    private static readonly Guid SubmittedStatusId = Guid.Parse("00000000-0000-0000-0000-000000000021");
    private static readonly Guid ApprovedStatusId = Guid.Parse("00000000-0000-0000-0000-000000000022");

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/{id}/status", Handle)
           .WithSummary("Update Timesheet Status")
           .WithRequestValidation<Request>();

    public sealed record Request(Guid StatusId, Guid? ApprovedBy, string? Comment);
    public sealed record Response(Guid Id);
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.StatusId).NotEmpty();
            RuleFor(x => x.Comment).MaximumLength(1000).When(x => x.Comment is not null);
        }
    }

    private static async Task<Results<Ok<Response>, BadRequest<string>, NotFound>> Handle(Guid id, [FromBody] Request request, AppDbContext dbContext, NotificationSender notificationSender, CancellationToken cancellationToken)
    {
        Data.Models.AttendanceTimesheet? timesheet = await dbContext.AttendanceTimesheets
            .Include(t => t.TimesheetStatus)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (timesheet is null)
        {
            return TypedResults.NotFound();
        }

        TimesheetStatus? newStatus = await dbContext.TimesheetStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StatusId, cancellationToken);

        if (newStatus is null)
        {
            return TypedResults.BadRequest($"Status with ID '{request.StatusId}' not found in database.");
        }

        Guid currentStatusId = timesheet.TimesheetStatusId;
        if (!IsValidStatusTransition(currentStatusId, request.StatusId))
        {
            string currentStatusName = timesheet.TimesheetStatus.Name;
            string newStatusName = newStatus.Name;
            return TypedResults.BadRequest($"Invalid status transition from '{currentStatusName}' (ID: {currentStatusId}) to '{newStatusName}' (ID: {request.StatusId}).");
        }

        timesheet.TimesheetStatusId = request.StatusId;
        timesheet.UpdatedAt = DateTime.UtcNow;

        if (request.StatusId == SubmittedStatusId && timesheet.SubmittedAt is null)
        {
            timesheet.SubmittedAt = DateTime.UtcNow;
        }
        else if (request.StatusId == ApprovedStatusId)
        {
            timesheet.ApprovedBy = request.ApprovedBy;
            timesheet.ApprovedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (currentStatusId != request.StatusId)
        {
            string notificationMessage = BuildNotificationMessage(
                timesheet.Year,
                timesheet.Month,
                timesheet.TimesheetStatus.Name,
                newStatus.Name,
                request.Comment);

            await notificationSender.SendAsync(timesheet.EmployeeId, notificationMessage, cancellationToken);
        }

        return TypedResults.Ok(new Response(timesheet.Id));
    }

    private static bool IsValidStatusTransition(Guid from, Guid to) => (from, to) switch
    {
        (Guid f, Guid t) when f == DraftStatusId && t == SubmittedStatusId => true,
        (Guid f, Guid t) when f == SubmittedStatusId && t == ApprovedStatusId => true,
        (Guid f, Guid t) when f == SubmittedStatusId && t == DraftStatusId => true,
        (Guid f, Guid t) when f == t => true,
        _ => false
    };

    private static string BuildNotificationMessage(int year, int month, string oldStatus, string newStatus, string? comment)
    {
        string monthName = new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("cs-CZ"));
        string message = $"Stav vašeho výkazu za {monthName} byl změněn z '{oldStatus}' na '{newStatus}'.";

        if (!string.IsNullOrWhiteSpace(comment))
        {
            message += $"\n\nKomentář: {comment}";
        }

        return message;
    }
}
