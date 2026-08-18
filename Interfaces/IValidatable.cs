namespace EduConnect.Interfaces;

// ISP: Segregated interface — only classes that require validation implement this.
// Not every entity needs validation, so it's kept separate.
public interface IValidatable
{
    Dictionary<string, string> ValidationErrors { get; }
    bool Validate();
}
