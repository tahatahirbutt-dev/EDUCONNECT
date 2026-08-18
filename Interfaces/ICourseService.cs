using EduConnect.Models;

namespace EduConnect.Interfaces;

// ISP: Course-specific operations separated from student/grade operations.
// DIP: Components depend on this abstraction, not the concrete CourseService.
public interface ICourseService : IRepository<Course>
{
    void EnrollStudent(Student student, Course course);
    void DropCourse(Student student, Course course);
    IEnumerable<Course> GetCoursesForFaculty(Guid facultyId);
    IEnumerable<Course> GetAvailableCourses(Guid studentId);

    // Event fired when enrollment changes — updates NavBar badge
    event Action<Student, Course>? OnEnrollmentChanged;
}
