var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

app.MapPost("/timesheets/import", async (IFormFile file) =>
{
    string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (extension is not ".xls" and not ".xlsx")
    {
        return Results.BadRequest("Unsupported file format. Supported formats: .xls, .xlsx.");
    }
    return Results.Ok();
})
.DisableAntiforgery();

app.Run();
