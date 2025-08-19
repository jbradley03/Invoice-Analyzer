using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages + API controllers
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// (Optional) increase upload limit (50 MB here)
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Map both Razor Pages and Controllers
app.MapRazorPages();
app.MapControllers();

app.Run();
