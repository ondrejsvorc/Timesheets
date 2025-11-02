using FluentValidation;
using Timesheets.Api;
using Timesheets.Api.Timesheets;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ICellParser, CellParser>();
builder.Services.AddSingleton<ITimesheetReader<AttendanceTimesheet>, AttendanceTimesheetReader>();
builder.Services.AddSingleton<IPublicHolidayProvider, CzechPublicHolidayProvider>();
builder.Services.AddTransient<ITimesheetImporter<AttendanceTimesheet>, AttendanceTimesheetImporter>();
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
