using AskTrackApi;
using AskTrackApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// 🔹 CORS Configuration
// ----------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("*");
    });
});

// ----------------------------
// 🔹 DB Context
// ----------------------------
builder.Services.AddDbContext<RemkDataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<GPSContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GPSConnection")));
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AskTrack API", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
// ----------------------------
// 🔹 Swagger
// ----------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ----------------------------
// 🔹 JWT Authentication
// ----------------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();

// ----------------------------
// 🔹 Register Services
// ----------------------------
builder.Services.AddControllers();
builder.Services.AddScoped<JwtService>();

// ----------------------------
// 🔹 Use Custom Host URL (Local IP)
// ----------------------------
builder.WebHost.UseUrls("http://localhost:5035", "http://0.0.0.0:5035");
if (!builder.Environment.IsProduction())
{
    builder.WebHost.UseUrls(
        "http://localhost:5035"         // Local machine
                                        // Remove "http://192.168.99.215:5035" if not assigned to your machine
    );
}


var app = builder.Build();

// ----------------------------
// 🔹 CORS Middleware
// ----------------------------
app.UseCors("AllowAll");

// Optional: Handle preflight requests
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "*");
        context.Response.StatusCode = 200;
        return;
    }
    await next();
});

// ----------------------------
// 🔹 Swagger UI
// ----------------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AskTrack API V1");
    c.RoutePrefix = "swagger"; // so it's at /swagger
});

// ----------------------------
// 🔹 Middleware Order: Auth then Map
// ----------------------------
app.UseAuthentication();  // 🔐 Validate tokens
app.UseAuthorization();   // 🔓 Check [Authorize] permissions

app.MapControllers();     // 🧭 Map routes

app.Run();