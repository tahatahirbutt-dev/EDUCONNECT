using EduConnect.Enums;

namespace EduConnect.Models;

// LSP: Faculty substitutes Person without breaking behavior.
public class Faculty : Person
{
    public string Department { get; set; } = string.Empty;
    public List<Course> AssignedCourses { get; set; } = new();

    public override UserRole GetRole() => UserRole.Faculty;
}
