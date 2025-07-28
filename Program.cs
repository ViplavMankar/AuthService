using AuthService.Data;
using AuthService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AuthService.Services;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var jwtKey = string.Empty;
var jwtIssuer = string.Empty;

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

    // JWT settings
    jwtKey = configuration["Jwt:Key"];
    jwtIssuer = configuration["Jwt:Issuer"];
}
else if (Environment.GetEnvironmentVariable("RENDER") != null)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("AUTH_DB_CONNECTION_STRING")));

    // JWT settings
    jwtKey = Environment.GetEnvironmentVariable("JWT_AUTH_KEY");
    jwtIssuer = Environment.GetEnvironmentVariable("JWT_AUTH_ISSUER");
}
else
{
    throw new Exception("No valid environment configuration found.");
}

if (jwtKey != null)
{
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.AddControllers();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        // options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        // options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.CallbackPath = "/signin-google";

        options.SignInScheme = null;

        options.Events.OnTicketReceived = async context =>
        {
            var email = context.Principal.FindFirstValue(ClaimTypes.Email);
            var name = context.Principal.FindFirstValue(ClaimTypes.Name);

            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var jwtService = context.HttpContext.RequestServices.GetRequiredService<IJwtTokenService>();

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                await userManager.CreateAsync(user);
            }

            var jwt = await jwtService.CreateToken(user);
            var refreshToken = jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken.Token;
            user.RefreshTokenExpiryTime = refreshToken.Expires;
            await userManager.UpdateAsync(user);

            // Redirect to GameStoreWeb with tokens
            var redirectUrl = "";
            if (builder.Environment.IsDevelopment())
                redirectUrl = $"https://localhost:7051/Account/Callback?token={jwt}&refreshToken={refreshToken.Token}";
            else if (Environment.GetEnvironmentVariable("RENDER") != null)
                redirectUrl = $"{Environment.GetEnvironmentVariable("GAMESTORE_URL")}Account/Callback?token={jwt}&refreshToken={refreshToken.Token}";
            else
            {
                throw new Exception("No valid environment configuration found.");
            }
            context.Response.Redirect(redirectUrl);
            context.HandleResponse();
        };
        // options.SignInScheme = IdentityConstants.ExternalScheme;

        // options.Events.OnTicketReceived = context =>
        // {
        //     // After Google signs them in, redirect them to process the login
        //     context.Response.Redirect("/api/auth/google/process");
        //     context.HandleResponse(); // Prevent default
        //     return Task.CompletedTask;
        // };
    });
}
else
{
    throw new Exception("JWT Key is not set in the environment variables.");
}

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by space and JWT token"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

if (Environment.GetEnvironmentVariable("RENDER") != null)
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.MigrateDbAsync();
await app.CreateAdminUser();

app.UseHttpsRedirection();
app.UseMiddleware<TokenLoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();