using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenStore.Api.Cart.Contracts;
using OpenStore.Api.Cart.Services;
using OpenStore.Api.Common.Auth;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Errors;
using OpenStore.Api.Common.Persistence;
using OpenStore.Api.Common.Time;
using OpenStore.Api.Categories.Contracts;
using OpenStore.Api.Categories.Services;
using OpenStore.Api.Products.Contracts;
using OpenStore.Api.Products.Services;
using OpenStore.Api.Stores.Contracts;
using OpenStore.Api.Stores.Services;
using OpenStore.Api.Tenancy.Contracts;
using OpenStore.Api.Tenancy.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

JwtSettings jwtSettings = builder.Configuration.GetSection("DevJwt").Get<JwtSettings>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            ModelValidationException exception = ModelValidationException.FromModelState(context.ModelState);
            ProblemDetails problem = ProblemDetailsBuilder.Build(context.HttpContext, exception);

            ObjectResult result = new(problem)
            {
                StatusCode = exception.StatusCode,
                ContentTypes = { "application/problem+json" }
            };

            return result;
        };
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();


WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{

    using IServiceScope scope = app.Services.CreateScope();
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler();

app.MapControllers();

await app.RunAsync();
