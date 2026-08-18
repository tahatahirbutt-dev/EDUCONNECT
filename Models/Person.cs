using EduConnect.Enums;

namespace EduConnect.Models;

// LSP: Anywhere a Person is expected, Student, Faculty, or Admin can substitute without breaking behavior.
// OCP: New roles (e.g., Librarian) can extend Person without changing existing code.
public abstract class Person
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // Simulated — plain text for mock auth

    // LSP: GetRole() behaves correctly for each subtype
    public abstract UserRole GetRole();
}
