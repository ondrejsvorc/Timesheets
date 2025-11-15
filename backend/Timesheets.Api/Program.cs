using FluentValidation;
using Timesheets.Api;
using Timesheets.Api.Data;
using Timesheets.Api.Notifications;
using Timesheets.Api.Timesheets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.CreateSchemaReferenceId = info =>
    {
        Type type = info.Type;
        if (type.IsGenericType)
        {
            Type genericArg = type.GetGenericArguments()[0];
            return genericArg.FullName?.Replace('+', '.') + "[]";
        }
        return type.FullName?.Replace('+', '.');
    };
});

builder.Services.AddDbContext<AppDbContext>();

builder.Services.AddSingleton<ICellParser, CellParser>();
builder.Services.AddSingleton<ITimesheetReader<AttendanceTimesheet>, AttendanceTimesheetReader>();
builder.Services.AddSingleton<ITimesheetReader<ProjectTimesheet>, ProjectTimesheetReader>();
builder.Services.AddSingleton<IPublicHolidayProvider, CzechPublicHolidayProvider>();
builder.Services.AddTransient<ITimesheetImporter<AttendanceTimesheet>, AttendanceTimesheetImporter>();
builder.Services.AddTransient<ITimesheetImporter<ProjectTimesheet>, ProjectTimesheetImporter>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddSignalR();
builder.Services.AddScoped<NotificationSender>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Timesheets API"));
}
app.UseHttpsRedirection();
app.MapEndpoints();
app.Run();
