using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Timesheets.Api.Data;
using Timesheets.Api.Data.Models;
using Timesheets.Api.Notifications.Endpoints;
using Xunit;

namespace Timesheets.Api.Tests.Integration.Notifications;

public class NotificationTests : BaseIntegrationTest
{
    public NotificationTests(CustomWebApplicationFactory factory) : base(factory) { }

    private WebApplicationFactory<Program> CreateAuthenticatedFactory() => Factory.WithWebHostBuilder(builder =>
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Enabled"] = "true"
            })));

    [Fact]
    public async Task GetEmployeeNotifications_DoesNotReturnForeignNotifications_AndReturnsMax50Sorted()
    {
        // Arrange
        Guid employee1Id = await SeedEmployeeAsync("NOTIF-EMP-1", "Notification Employee 1");
        Guid employee2Id = await SeedEmployeeAsync("NOTIF-EMP-2", "Notification Employee 2");

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Add 51 notifications for employee 1
            for (int i = 0; i < 51; i++)
            {
                db.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employee1Id,
                    Message = $"Message {i}",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            // Add 1 notification for employee 2
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee2Id,
                Message = "Foreign Message",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        // Act - Request as employee 1
        using WebApplicationFactory<Program> factory = CreateAuthenticatedFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/notifications");
        request.Headers.Add(TestAuthHandler.PersonalNumberHeader, "NOTIF-EMP-1");
        var response = await client.SendAsync(request);

        var result = await response.Content.ReadFromJsonAsync<List<GetEmployeeNotifications.Response>>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(50, result.Count); // Max 50
        Assert.DoesNotContain(result, n => n.Message == "Foreign Message"); // No foreign notification
        Assert.True(result[0].CreatedAt >= result[1].CreatedAt); // Sorted descending
    }

    [Fact]
    public async Task MarkNotificationAsRead_ForeignNotification_ReturnsNotFoundAndRemainsUnread()
    {
        // Arrange
        Guid employee1Id = await SeedEmployeeAsync("NOTIF-EMP-3", "Notification Employee 3");
        Guid employee2Id = await SeedEmployeeAsync("NOTIF-EMP-4", "Notification Employee 4");

        Guid foreignNotificationId = Guid.NewGuid();
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Notifications.Add(new Notification
            {
                Id = foreignNotificationId,
                EmployeeId = employee2Id,
                Message = "Foreign Message",
                IsRead = false
            });
            await db.SaveChangesAsync();
        }

        // Act - Try to mark employee 2's notification as employee 1
        using WebApplicationFactory<Program> factory = CreateAuthenticatedFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/notifications/{foreignNotificationId}/read");
        request.Headers.Add(TestAuthHandler.PersonalNumberHeader, "NOTIF-EMP-3");
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notification = await db.Notifications.FindAsync(foreignNotificationId);
            Assert.NotNull(notification);
            Assert.False(notification.IsRead); // Remains unread
        }
    }

    [Fact]
    public async Task MarkAllNotificationsAsRead_OnlyMarksOwnNotifications()
    {
        // Arrange
        Guid employee1Id = await SeedEmployeeAsync("NOTIF-EMP-5", "Notification Employee 5");
        Guid employee2Id = await SeedEmployeeAsync("NOTIF-EMP-6", "Notification Employee 6");

        Guid myNotificationId = Guid.NewGuid();
        Guid foreignNotificationId = Guid.NewGuid();

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Notifications.Add(new Notification
            {
                Id = myNotificationId,
                EmployeeId = employee1Id,
                Message = "My Message",
                IsRead = false
            });
            db.Notifications.Add(new Notification
            {
                Id = foreignNotificationId,
                EmployeeId = employee2Id,
                Message = "Foreign Message",
                IsRead = false
            });
            await db.SaveChangesAsync();
        }

        // Act - Mark all as read for employee 1
        using WebApplicationFactory<Program> factory = CreateAuthenticatedFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/notifications/read-all");
        request.Headers.Add(TestAuthHandler.PersonalNumberHeader, "NOTIF-EMP-5");
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var myNotif = await db.Notifications.FindAsync(myNotificationId);
            var foreignNotif = await db.Notifications.FindAsync(foreignNotificationId);

            Assert.True(myNotif!.IsRead); // Mine is marked as read
            Assert.False(foreignNotif!.IsRead); // Foreign remains unread
        }
    }
}
