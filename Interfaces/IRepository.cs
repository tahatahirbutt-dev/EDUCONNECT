namespace EduConnect.Interfaces;

// OCP: Open for extension — new entity types can be added (IRepository<Faculty>, IRepository<Course>)
// without modifying the repository infrastructure.
// LSP: IRepository<Student> and IRepository<Course> are interchangeable where IRepository<T> is expected.
public interface IRepository<T>
{
    IEnumerable<T> GetAll();
    T? GetById(Guid id);
    void Add(T entity);
    void Update(T entity);
    void Delete(Guid id);
}
