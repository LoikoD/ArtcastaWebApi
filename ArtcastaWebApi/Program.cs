using ArtcastaWebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Bearer Authentication with JWT Token",
        Type = SecuritySchemeType.Http
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new List<string>()
        }
    });
});


// JWT authentication
var tokenValidationParameters = new TokenValidationParameters()
{
    ValidateActor = true,
    ValidateAudience = false,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = builder.Configuration["Jwt:Issuer"],
    //ValidAudience = builder.Configuration["Jwt:Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
    ClockSkew = TimeSpan.Zero
};
builder.Services.AddSingleton(tokenValidationParameters);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = tokenValidationParameters;
});
builder.Services.AddAuthorization();

// Enable CORS
builder.Services.AddCors(c =>
{
    c.AddPolicy("AllowOrigin", options =>
    {
        options.WithOrigins("https://localhost:3000", "https://192.168.0.199:3000", "https://cosmic-fudge-8b0b1a.netlify.app");
        options.AllowAnyMethod();
        options.AllowAnyHeader();
        options.AllowCredentials();
    });
});

//builder.Services.AddControllersWithViews()
//    .AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft
//    .Json.ReferenceLoopHandling.Ignore)
//    .AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());

// JSON Serializer
builder.Services.AddControllers()
    .AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft
    .Json.ReferenceLoopHandling.Ignore)
    .AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());

builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<ITableService, TableService>();
builder.Services.AddSingleton<IRolesService, RolesService>();
builder.Services.AddSingleton<IAccessPointsService, AccessPointsService>();

var app = builder.Build();

app.UseSwagger();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();


app.UseCors("AllowOrigin");

//app.Use(async (context, next) =>
//{
//    var token = context.Request.Cookies["access_token"];
//    if (!string.IsNullOrEmpty(token)) context.Request.Headers.Add("Authorization", "Bearer " + token);
//    await next();
//});

app.UseAuthorization();
app.UseAuthentication();

app.MapControllers();

app.UseSwaggerUI();

app.Run();
