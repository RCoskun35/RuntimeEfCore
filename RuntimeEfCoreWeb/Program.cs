using Microsoft.OpenApi.Models;
using RuntimeEfCoreWeb;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
DynamicContextExtensions.DynamicContext(connectionString!);
// Add services to the container.


builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();

var dbContext = DynamicContextExtensions.dynamicContext;
builder.Services.AddDynamicCrudEndpoints(dbContext);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
