using EduConnect.Enums;

namespace EduConnect.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }        // Target user
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string TypeBadgeClass => Type switch
    {
        NotificationType.Enrollment   => "bg-primary",
        NotificationType.GradePosted  => "bg-success",
        NotificationType.Announcement => "bg-warning text-dark",
        _ => "bg-secondary"
    };

    public string TypeIcon => Type switch
    {
        NotificationType.Enrollment   => "bi-person-check",
        NotificationType.GradePosted  => "bi-bar-chart",
        NotificationType.Announcement => "bi-megaphone",
        _ => "bi-bell"
    };
}
