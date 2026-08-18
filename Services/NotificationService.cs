using EduConnect.Enums;
using EduConnect.Interfaces;
using EduConnect.Models;
using Microsoft.Data.Sqlite;

namespace EduConnect.Services;

/// <summary>
/// SRP: Notification management only.
/// Notifications are persisted to the local SQLite .db file.
/// The event Action<Notification> OnNewNotification remains in-memory
/// because Blazor reactivity (SignalR) does not need DB for real-time UI updates.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly string _connectionString;

    // C# event stays in-memory — components subscribe/unsubscribe via IDisposable
    public event Action<Notification>? OnNewNotification;

    public NotificationService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public IEnumerable<Notification> GetNotificationsForUser(Guid userId)
    {
        var list = new List<Notification>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(@"
            SELECT * FROM Notifications
            WHERE UserId = @UserId
            ORDER BY CreatedAt DESC", conn);
        cmd.Parameters.AddWithValue("@UserId", userId.ToString());
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(MapToNotification(r));
        return list;
    }

    public void SendNotification(Notification notification)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(@"
            INSERT INTO Notifications (Id, UserId, Message, Type, IsRead, CreatedAt)
            VALUES (@Id, @UserId, @Message, @Type, 0, @CreatedAt)", conn);
        cmd.Parameters.AddWithValue("@Id",        notification.Id.ToString());
        cmd.Parameters.AddWithValue("@UserId",    notification.UserId.ToString());
        cmd.Parameters.AddWithValue("@Message",   notification.Message);
        cmd.Parameters.AddWithValue("@Type",      (int)notification.Type);
        cmd.Parameters.AddWithValue("@CreatedAt", notification.CreatedAt);
        cmd.ExecuteNonQuery();

        // Fire event — NotificationBell re-renders via StateHasChanged()
        OnNewNotification?.Invoke(notification);
    }

    public void BroadcastToRole(
        IEnumerable<Person> users, UserRole role, string message, NotificationType type)
    {
        foreach (var user in users.Where(u => u.GetRole() == role))
        {
            SendNotification(new Notification
            {
                UserId  = user.Id,
                Message = message,
                Type    = type
            });
        }
    }

    public void MarkAsRead(Guid notificationId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(
            "UPDATE Notifications SET IsRead = 1 WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", notificationId.ToString());
        cmd.ExecuteNonQuery();
    }

    public int GetUnreadCount(Guid userId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = new SqliteCommand(@"
            SELECT COUNT(*) FROM Notifications
            WHERE UserId = @UserId AND IsRead = 0", conn);
        cmd.Parameters.AddWithValue("@UserId", userId.ToString());
        return (int)(long)cmd.ExecuteScalar()!;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Notification MapToNotification(SqliteDataReader r) => new()
    {
        Id        = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        UserId    = Guid.Parse(r.GetString(r.GetOrdinal("UserId"))),
        Message   = r.GetString(r.GetOrdinal("Message")),
        Type      = (NotificationType)r.GetInt32(r.GetOrdinal("Type")),
        IsRead    = r.GetBoolean(r.GetOrdinal("IsRead")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt"))
    };
}
