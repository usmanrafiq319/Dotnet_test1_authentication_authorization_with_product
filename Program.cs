using Amazon.S3;
using Dotnet_test1_authentication_authorization_with_product.Configuration;
using Dotnet_test1_authentication_authorization_with_product.Data;
using Dotnet_test1_authentication_authorization_with_product.Entities;
using Dotnet_test1_authentication_authorization_with_product.Hubs;
using Dotnet_test1_authentication_authorization_with_product.Services;
using Dotnet_test1_authentication_authorization_with_product.Services.Chat;
using Dotnet_test1_authentication_authorization_with_product.Services.Groq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// BASIC SERVICES
// ============================================
builder.Services.AddControllers();
builder.Services.AddOpenApi();
// ============================================
// GROOK SETTING
// ============================================
builder.Services
    .AddOptions<GroqOptions>()
    .Bind(
        builder.Configuration.GetSection(
            GroqOptions.SectionName
        )
    )
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(
                options.ApiKey
            ),
        "Groq API key is required."
    )
    .ValidateOnStart();

// ============================================
// Register business services
// ============================================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IR2ImageService, R2ImageService>();
builder.Services.AddScoped<IChatService, ChatService>();
//builder.Services.AddScoped<IGroqChatService, GroqChatService>();
builder.Services.AddSignalR();


builder.Services.AddHttpClient<IGroqChatService,GroqChatService>(
    (serviceProvider, httpClient) =>
    {
        var options =
            serviceProvider
                .GetRequiredService<
                    IOptions<GroqOptions>
                >()
                .Value;

        httpClient.BaseAddress =
            new Uri(options.BaseUrl);

        httpClient.Timeout =
            TimeSpan.FromSeconds(30);
    }
);

// ============================================
// R2 STORAGE CONFIGURATION
// ============================================
builder.Services.Configure<R2Storage>(
    builder.Configuration.GetSection("R2Storage")
);

// ============================================
// 2. Register AmazonS3Client as a SINGLETON to reuse network connections
// ============================================
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var r2Options = sp.GetRequiredService<IOptions<R2Storage>>().Value;

    var config = new AmazonS3Config
    {
        ServiceURL = r2Options.Endpoint,
        ForcePathStyle = true
    };

    var credentials = new Amazon.Runtime.BasicAWSCredentials(
        r2Options.AccessKeyId,
        r2Options.SecretAccessKey
    );

    return new AmazonS3Client(credentials, config);
});
// ============================================
// CORS CONFIGURATION (Single, Secure Policy)
// ============================================
builder.Services.AddCors(options =>
{
    // In development, allow localhost sources with credentials
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("AllowAngular", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:4200",
                    "http://localhost:4201",
                    "http://localhost:3000"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    }
    else // In production, ONLY allow your actual domains
    {
        options.AddPolicy("AllowAngular", policy =>
        {
            policy.WithOrigins(
                    "https://yourdomain.com",
                    "https://www.yourdomain.com",
                    "https://api.yourdomain.com"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    }
});

// ============================================
// DATABASE 
// ============================================
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UserDatabase"))
);




// ============================================
// CACHE Storage for otp
// ============================================

builder.Services.AddMemoryCache();
builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection("OtpSettings"));
builder.Services.AddHostedService<OtpCleanupService>();


// ============================================
// Singleton utilities
// ============================================

// CHANGE THIS from AddSingleton to AddTransient
builder.Services.AddTransient<JwtSecurityTokenHandler>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// ============================================
// JWT AUTHENTICATION
// ============================================
builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme
    )
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration[
                        "AppSettings:Issuer"
                    ],

                ValidAudience =
                    builder.Configuration[
                        "AppSettings:Audience"
                    ],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration[
                                "AppSettings:Token"
                            ]!
                        )
                    ),

                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken =
                    context.Request.Query["access_token"];

                var path =
                    context.HttpContext.Request.Path;

                if (
                    !string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/chathub")
                )
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// ============================================
// EMAIL CONFIGURATION
// ============================================

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings")
);

var app = builder.Build();

// ============================================
// MIDDLEWARE PIPELINE (Order is critical!)
// ============================================

// 1. Apply CORS first so preflight requests are answered
app.UseCors("AllowAngular");

// 2. Enforce HTTPS
app.UseHttpsRedirection();

// 3. Authenticate the JWT token (Decodes 'Who' the user is)
app.UseAuthentication();

// 4. Authorize the user (Checks 'What' they are allowed to do)
app.UseAuthorization();

// 5. Open API & Documentation (Dev only)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// 6. Map API Endpoints
app.MapControllers();
app.MapHub<ChatHub>("/chathub");
app.Run();