using TreeGrouping.Application.CategoryService;
using TreeGrouping.Application.DbService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();

builder.Services.AddScoped<CategoryCacheService>();
builder.Services.AddScoped<CategoryFilterService>();
builder.Services.AddScoped<CategoryTreeService>();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Category}/{action=Index}")
    .WithStaticAssets();


app.Run();