using System.Text;
using CampusEats.Api.Data;
using CampusEats.Api.Models;
using CampusEats.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var databaseUrl = builder.Configuration["DATABASE_URL"];
var connectionString = !string.IsNullOrWhiteSpace(databaseUrl)
    ? GetDatabaseConnectionString(databaseUrl)
    : builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure the database using DATABASE_URL or ConnectionStrings__Default.");
}

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("https://campuseats-dashboard-nu.vercel.app")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste ONLY the token - no 'Bearer ' prefix."
        });

    c.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference =
                        new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type =
                                Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});
builder.Services.AddScoped<IMenuService, MenuService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var jwt = builder.Configuration.GetSection("Jwt");

var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],

            ValidateAudience = true,
            ValidAudience = jwt["Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(key),

            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!db.MenuItems.Any())
    {
        db.MenuItems.AddRange(
            new MenuItem
            {
                Name = "Kottu Roti",
                Price = 750m,
                Category = "Mains"
            },
            new MenuItem
            {
                Name = "Fried Rice",
                Price = 850m,
                Category = "Mains"
            },
            new MenuItem
            {
                Name = "Watalappan",
                Price = 350m,
                Category = "Dessert"
            }
        );

        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string GetDatabaseConnectionString(string databaseUrl)
{
    if (!databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        && !databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return databaseUrl;
    }

    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
    {
        throw new InvalidOperationException("DATABASE_URL must be a valid PostgreSQL URL.");
    }

    var credentials = uri.UserInfo.Split(':', 2);
    var databaseName = uri.AbsolutePath.Trim('/');
    if (credentials.Length != 2 || string.IsNullOrWhiteSpace(uri.Host)
        || string.IsNullOrWhiteSpace(databaseName))
    {
        throw new InvalidOperationException(
            "DATABASE_URL must include the PostgreSQL host, database, username, and password.");
    }

    return new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = Uri.UnescapeDataString(databaseName),
        Username = Uri.UnescapeDataString(credentials[0]),
        Password = Uri.UnescapeDataString(credentials[1]),
        SslMode = SslMode.Require
    }.ConnectionString;
}
