using EduConnect.Enums;

namespace EduConnect.Models;

// LSP: Admin substitutes Person without breaking behavior.
public class Admin : Person
{
    public override UserRole GetRole() => UserRole.Admin;
}
