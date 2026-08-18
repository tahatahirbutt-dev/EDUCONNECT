namespace EduConnect.Exceptions;

/// <summary>
/// Thrown when attempting to delete a student who has active course enrollments.
/// Caught in the component and displayed as an error — student is NOT deleted.
/// </summary>
public class StudentHasActiveEnrollmentsException : Exception
{
    public StudentHasActiveEnrollmentsException(string studentName)
        : base($"Cannot delete '{studentName}' — they have active course enrollments. Drop all courses first.")
    { }
}

/// <summary>
/// Thrown when a student attempts to enroll in a course that has reached MaxCapacity.
/// Caught in the component and displayed as a Bootstrap alert.
/// </summary>
public class CourseFullException : Exception
{
    public CourseFullException(string courseTitle)
        : base($"Cannot enroll: '{courseTitle}' has reached its maximum capacity.")
    { }
}

/// <summary>
/// Thrown when a student tries to re-enroll in a dropped course in the same semester.
/// Enforced in the service layer, not the component.
/// </summary>
public class AlreadyDroppedCourseException : Exception
{
    public AlreadyDroppedCourseException(string courseTitle)
        : base($"'{courseTitle}' was dropped this semester and cannot be re-enrolled.")
    { }
}
