using TreeGrouping.Application.CategoryService;
using TreeGrouping.Application.CategoryService.CacheHelper;
using TreeGrouping.Application.DbService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();

builder.Services.AddScoped<ICategoryCacheHelper, CategoryCacheHelper>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

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