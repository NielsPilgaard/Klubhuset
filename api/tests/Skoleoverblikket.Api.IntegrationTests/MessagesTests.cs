using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Skoleoverblikket.Api.Controllers;
using Skoleoverblikket.Api.Data;
using Skoleoverblikket.Api.IntegrationTests.Infrastructure;
using Skoleoverblikket.Api.Models;
using Skoleoverblikket.Api.Services;

namespace Skoleoverblikket.Api.IntegrationTests;

/// <summary>
/// Integration tests for MessagesController.
/// Covers:
///   - POST /api/v1/messages: staff→parent (201), parent→parent with consent (201),
///     parent→parent without consent (403), no sender record (403).
///   - GET /api/v1/messages/inbox: staff inbox with data (200), parent empty inbox (200).
/// </summary>
[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
public sealed class MessagesTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    private readonly ApiFactory _factory = factory;
    private readonly Guid _tenantId = Guid.NewGuid();
    private HttpClient _adminClient = null!;

    [Before(HookType.Class)]
    public async Task SetUp()
    {
        await TestDataBuilder.CreateSchoolAsync(_factory.Services, _tenantId);
        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Add("X-Test-TenantId", _tenantId.ToString());
        _adminClient.DefaultRequestHeaders.Add("X-Test-Roles", "admin");
        _adminClient.DefaultRequestHeaders.Add("X-Test-Subject", "admin-subject");
    }

    // ── Private helpers ──────────────────────────────────────────────────────────

    private HttpClient CreateStaffClient(string subject, bool isAdmin = false)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-TenantId", _tenantId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", isAdmin ? "admin" : "user");
        client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
        return client;
    }

    private HttpClient CreateParentClient(string subject)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-TenantId", _tenantId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "parent");
        client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
        return client;
    }

    private async Task<Parent> CreateParentAsync(
        string keycloakSubject,
        string name = "Dorte Testsen",
        bool shareContactInfo = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var parent = new Parent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = name,
            Email = $"{keycloakSubject}@test.dk",
            KeycloakSubject = keycloakSubject,
            ShareContactInfo = shareContactInfo,
        };
        db.Parents.Add(parent);
        await db.SaveChangesAsync();
        return parent;
    }

    private async Task<Message> CreateMessageAsync(
        Guid senderId,
        RecipientType senderType,
        Guid recipientId,
        RecipientType recipientType,
        string subject = "Test emne",
        string body = "Test besked")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            SenderId = senderId,
            SenderType = senderType,
            RecipientId = recipientId,
            RecipientType = recipientType,
            Subject = subject,
            Body = body,
            SentAt = DateTimeOffset.UtcNow,
        };
        db.Messages.Add(message);
        await db.SaveChangesAsync();
        return message;
    }

    // ── POST /api/v1/messages ─────────────────────────────────────────────────────

    [Test]
    public async Task SendMessage_StaffToParent_Returns201()
    {
        const string senderSubject = "msg-staff-sender";
        await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId,
            name: "Lars Lærer", keycloakSubject: senderSubject);
        var recipient = await CreateParentAsync("msg-staff-to-parent-recipient", name: "Britta Forælder");

        var request = new MessagesController.SendMessageRequest(
            recipient.Id,
            RecipientType.Parent,
            "Hej fra lærer",
            "Dette er en besked fra en lærer til en forælder.");

        using var client = CreateStaffClient(senderSubject);
        var response = await client.PostAsJsonAsync("/api/v1/messages", request, JsonOpts);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    [Test]
    public async Task SendMessage_ParentToParent_WithConsent_Returns201()
    {
        const string senderSubject = "msg-parent-sender-consent";
        await CreateParentAsync(senderSubject, name: "Anne Afsender", shareContactInfo: true);
        var recipient = await CreateParentAsync("msg-parent-recipient-consent", name: "Bo Modtager", shareContactInfo: true);

        var request = new MessagesController.SendMessageRequest(
            recipient.Id,
            RecipientType.Parent,
            "Hej fra forælder med samtykke",
            "Begge forældre har givet samtykke til deling af kontaktoplysninger.");

        using var client = CreateParentClient(senderSubject);
        var response = await client.PostAsJsonAsync("/api/v1/messages", request, JsonOpts);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    [Test]
    public async Task SendMessage_ParentToParent_WithoutConsent_Returns403()
    {
        const string senderSubject = "msg-parent-sender-no-consent";
        await CreateParentAsync(senderSubject, name: "Carla Afsender", shareContactInfo: true);
        var recipient = await CreateParentAsync("msg-parent-recipient-no-consent", name: "Dan Modtager", shareContactInfo: false);

        var request = new MessagesController.SendMessageRequest(
            recipient.Id,
            RecipientType.Parent,
            "Hej fra forælder uden samtykke",
            "Modtageren har ikke givet samtykke til deling af kontaktoplysninger.");

        using var client = CreateParentClient(senderSubject);
        var response = await client.PostAsJsonAsync("/api/v1/messages", request, JsonOpts);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task SendMessage_NoSenderRecord_Returns403()
    {
        // Authenticated user whose Keycloak subject has no Staff or Parent row in DB
        const string unknownSubject = "msg-unknown-sender-no-record";
        var recipient = await CreateParentAsync("msg-no-record-recipient", name: "Eva Modtager");

        var request = new MessagesController.SendMessageRequest(
            recipient.Id,
            RecipientType.Parent,
            "Besked fra ukendt bruger",
            "Denne bruger eksisterer ikke i databasen.");

        using var client = CreateStaffClient(unknownSubject);
        var response = await client.PostAsJsonAsync("/api/v1/messages", request, JsonOpts);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    // ── GET /api/v1/messages/inbox ────────────────────────────────────────────────

    [Test]
    public async Task GetInbox_Staff_Returns200()
    {
        const string recipientSubject = "msg-inbox-staff-recipient";
        var recipient = await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId,
            name: "Frede Modtager", keycloakSubject: recipientSubject);

        var sender = await TestDataBuilder.CreateStaffAsync(_factory.Services, _tenantId,
            name: "Gitte Afsender", keycloakSubject: "msg-inbox-staff-sender");

        await CreateMessageAsync(
            sender.Id,
            RecipientType.Staff,
            recipient.Id,
            RecipientType.Staff,
            subject: "Indbakke testbesked til medarbejder",
            body: "Denne besked skal vises i medarbejderens indbakke.");

        using var client = CreateStaffClient(recipientSubject);
        var response = await client.GetAsync("/api/v1/messages/inbox");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var inbox = await response.Content.ReadFromJsonAsync<List<MessagesController.InboxMessageDto>>(JsonOpts);
        await Assert.That(inbox).IsNotNull();
        await Assert.That(inbox!.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(inbox.Any(m => m.Subject == "Indbakke testbesked til medarbejder")).IsTrue();
    }

    [Test]
    public async Task GetInbox_ParentWithNoMessages_Returns200Empty()
    {
        const string parentSubject = "msg-inbox-parent-empty";
        await CreateParentAsync(parentSubject, name: "Hanne Tom Indbakke");

        using var client = CreateParentClient(parentSubject);
        var response = await client.GetAsync("/api/v1/messages/inbox");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var inbox = await response.Content.ReadFromJsonAsync<List<MessagesController.InboxMessageDto>>(JsonOpts);
        await Assert.That(inbox).IsNotNull();
        await Assert.That(inbox!.Count).IsEqualTo(0);
    }
}
