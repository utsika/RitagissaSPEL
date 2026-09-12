using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connString = builder.Configuration.GetConnectionString("Supabase");
builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(connString).Build());


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


// TILLFÄLLIG TESTKOD — ta bort igen efter du sett att det funkar
using (var testConn = new NpgsqlConnection(connString))
{
    await testConn.OpenAsync();
    using var testCmd = new NpgsqlCommand("SELECT version()", testConn);
    var version = (string)await testCmd.ExecuteScalarAsync();
    Console.WriteLine($"✅ Anslutning OK: {version}");
}
app.Run();
