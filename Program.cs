using EduConnect;
using EduConnect.Data;
using EduConnect.Interfaces;
using EduConnect.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── DIP: All services registered as interfaces ──────────────────────────────
// Components inject interfaces — never instantiate services with new().
// IConfiguration is automatically available from appsettings.json.

builder.Services.AddScoped<AuthStateService>();

// StudentService registered both as itself (GradeService needs it directly)
// and as IStudentService (pages inject this interface)
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<IStudentService>(sp => sp.GetRequiredService<StudentService>());

builder.Services.AddScoped<ICourseService,      CourseService>();
builder.Services.AddScoped<IGradeService,       GradeService>();
builder.Services.AddScoped<INotificationService,NotificationService>();

var app = builder.Build();

// Creates EduConnect.db + schema on first run, and seeds demo accounts
// if the database is empty. Safe to leave in — it never touches an
// already-populated database.
DbInitializer.Initialize(app.Configuration.GetConnectionString("DefaultConnection")!);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
