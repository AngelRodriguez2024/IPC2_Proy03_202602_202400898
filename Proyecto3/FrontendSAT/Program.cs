var builder = WebApplication.CreateBuilder(args);

// Agregar servicios para Razor Pages
builder.Services.AddRazorPages();

// Registrar HttpClient para consumir la API de BackendSAT
builder.Services.AddHttpClient("BackendSAT", client =>
{
    client.BaseAddress = new Uri("http://localhost:5130/");
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

app.MapRazorPages();

app.Run();