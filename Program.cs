using LieAsocial.Components;
using LieAsocial;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

    builder.Services.AddScoped(provider =>
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("OdbcConnection")
            ?? throw new InvalidOperationException("Connection string 'OdbcConnection' not found.");
        return new UserService(connectionString);
    });

    builder.Services.AddScoped(provider =>
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("OdbcConnection")
            ?? throw new InvalidOperationException("Connection string 'OdbcConnection' not found.");
        return new PostService(connectionString);
    });

builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();