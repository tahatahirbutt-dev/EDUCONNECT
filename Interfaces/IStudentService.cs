using EduConnect.Models;

namespace EduConnect.Interfaces;

// ISP: IStudentService extends IRepository<Student> but does NOT include grade-related methods.
// Those belong to IGradeService. No class is forced to implement unneeded methods.
public interface IStudentService : IRepository<Student>
{
    IEnumerable<Student> SearchStudents(string query);
    event Action<Student>? OnStudentUpdated;
}
