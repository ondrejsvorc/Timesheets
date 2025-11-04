using FluentValidation;
using Timesheets.Api;
using Timesheets.Api.Timesheets;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.CustomSchemaIds(type => type.FullName?.Replace('+', '.')));
builder.Services.AddTransient<ITimesheetReader<AttendanceTimesheet>, AttendanceTimesheetReader>();
builder.Services.AddSingleton<ITimesheetReader<ProjectTimesheet>, ProjectTimesheetReader>();
builder.Services.AddTransient<IPublicHolidayProvider, CzechPublicHolidayProvider>();
builder.Services.AddTransient<ITimesheetImporter<AttendanceTimesheet>, AttendanceTimesheetImporter>();
builder.Services.AddTransient<ITimesheetImporter<ProjectTimesheet>, ProjectTimesheetImporter>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.MapEndpoints();
app.Run();