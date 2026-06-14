using System.Net;
using System.Net.Http.Json;
using System.Text;
using Timesheets.Api.Projects.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Projects;

public class CreateProjectTests : BaseIntegrationTest
{
    public CreateProjectTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateProject_WithValidData_ReturnsCreated()
    {
        CreateProject.Request request = new("Isolated Create Test", "REG-CREATE-001", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10));
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/projects", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithEmptyName_ReturnsBadRequest()
    {
        CreateProject.Request request = new("", "REG-CREATE-002", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10));
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/projects", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithLongName_ReturnsBadRequest()
    {
        CreateProject.Request request = new(new string('A', 201), "REG-CREATE-003", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10));
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/projects", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithLongRegistrationNumber_ReturnsBadRequest()
    {
        CreateProject.Request request = new("Valid Name", new string('B', 101), DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10));
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/projects", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithStartDateGreaterEndDate_ReturnsBadRequest()
    {
        DateTime date = DateTime.UtcNow.Date;
        CreateProject.Request request = new("Valid Name", "REG-CREATE-005", date.AddDays(1), date);
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/projects", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithNullEndDate_ReturnsCreated()
    {
        CreateProject.Request request = new("Valid Name", "REG-CREATE-006", DateTime.UtcNow.Date, null);
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/projects", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithDuplicateRegistrationNumber_ReturnsBadRequest()
    {
        CreateProject.Request first = new("First Project", "REG-DUP-001", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10));
        HttpResponseMessage firstResponse = await Client.PostAsJsonAsync("/api/projects", first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        CreateProject.Request duplicate = new("Second Project", "REG-DUP-001", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10));
        HttpResponseMessage duplicateResponse = await Client.PostAsJsonAsync("/api/projects", duplicate);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        Assert.Contains("existuje", await duplicateResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateProject_WithDuplicateName_ReturnsBadRequest()
    {
        CreateProject.Request first = new("Duplicate Name Project", "REG-DUP-002", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10));
        HttpResponseMessage firstResponse = await Client.PostAsJsonAsync("/api/projects", first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        CreateProject.Request duplicate = new("Duplicate Name Project", "REG-DUP-003", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(10));
        HttpResponseMessage duplicateResponse = await Client.PostAsJsonAsync("/api/projects", duplicate);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        Assert.Contains("existuje", await duplicateResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateProject_WithUtcMidnightJsonString_ReturnsCreated()
    {
        using StringContent content = new("""{"name":"Utc Midnight Project","registrationNumber":"REG-CREATE-007","startDate":"2026-01-01T00:00:00.000Z","endDate":null}""", Encoding.UTF8, "application/json");
        HttpResponseMessage response = await Client.PostAsync("/api/projects", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
