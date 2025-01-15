using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;
using RuntimeEfCoreWeb;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
DynamicContextExtensions.DynamicContext(connectionString!);
// Add services to the container.

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllersWithViews().AddOData(opt =>
{
    var edmModel = DynamicContextExtensions.GetDynamicEdmModel(DynamicContextExtensions.dynamicContext);
    opt.AddRouteComponents("odata", edmModel)
        .Filter()
        .Select()
        .Count()
        .Expand()
        .OrderBy()
        .SetMaxTop(null);
        
});

var dbContext = DynamicContextExtensions.dynamicContext;
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
