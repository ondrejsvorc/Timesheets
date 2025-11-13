using FluentValidation;
using Timesheets.Api;
using Timesheets.Api.Timesheets;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ICellParser, CellParser>();
builder.Services.AddSingleton<ITimesheetReader<AttendanceTimesheet>, AttendanceTimesheetReader>();
builder.Services.AddSingleton<ITimesheetReader<ProjectTimesheet>, ProjectTimesheetReader>();
builder.Services.AddSingleton<IPublicHolidayProvider, CzechPublicHolidayProvider>();
builder.Services.AddTransient<ITimesheetImporter<AttendanceTimesheet>, AttendanceTimesheetImporter>();
builder.Services.AddTransient<ITimesheetImporter<ProjectTimesheet>, ProjectTimesheetImporter>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Timesheets API"));
}
app.UseHttpsRedirection();
app.MapEndpoints();
app.Run();
