using EduConnect.Enums;
using EduConnect.Models;

namespace EduConnect.Interfaces;

// SRP: Only manages notifications. No mixing with student or grade logic.
public interface INotificationService
{
    IEnumerable<Notification> GetNotificationsForUser(Guid userId);
    void SendNotification(Notification notification);
    void BroadcastToRole(IEnumerable<Person> users, UserRole role, string message, NotificationType type);
    void MarkAsRead(Guid notificationId);
    int GetUnreadCount(Guid userId);

    // C# event (Action<T>) — components subscribe/unsubscribe via IDisposable
    event Action<Notification>? OnNewNotification;
}
