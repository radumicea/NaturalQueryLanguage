using NaturalQueryLanguage.Business;
using NaturalQueryLanguage.Clients;

const string corsPolicy = "_defaultCorsPolicy";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        policy.AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(origin => true)
            .AllowCredentials();
    });
});

RegisterClients();
RegisterServices();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseHsts();
app.UseCors(corsPolicy);

app.MapControllers();

app.Run();

void RegisterServices()
{
    builder.Services.AddSingleton<DbSchemaService>();
    builder.Services.AddSingleton<SqlGeneratorService>();
}

void RegisterClients()
{
    builder.Services.AddSingleton<ChatGptClient>();
    builder.Services.AddSingleton<StorageClient>();
}