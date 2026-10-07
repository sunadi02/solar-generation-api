using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SolarGenerationApi.Data;
using SolarGenerationApi.Errors;
using SolarGenerationApi.Security;
using SolarGenerationApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
    {
        options.ReturnHttpNotAcceptable = true; // 406
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var details = context.ModelState
                .Where(e => e.Value != null && e.Value.Errors.Count > 0)
                .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());
            return Err.Make(400, "VALIDATION_ERROR", "One or more fields are invalid.", details);
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SolarGenerationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<TokenService>();

var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var scope in new[] { "readings:read", "installation:write", "admin:write" })
    {
        options.AddPolicy(scope, policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(ctx => ctx.User.HasScope(scope)));
    }
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SolarGenerationDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);
}

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.StatusCode = 500;
    await context.Response.WriteAsJsonAsync(new ApiError("INTERNAL_ERROR", "An unexpected error occurred."));
}));

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    var (code, message) = response.StatusCode switch
    {
        400 => ("BAD_REQUEST", "The request is invalid."),
        401 => ("UNAUTHENTICATED", "A valid bearer token is required."),
        403 => ("FORBIDDEN", "You do not have permission to perform this action."),
        404 => ("NOT_FOUND", "The requested resource was not found."),
        405 => ("METHOD_NOT_ALLOWED", "This method is not allowed on this resource."),
        406 => ("NOT_ACCEPTABLE", "Only application/json responses are available."),
        415 => ("UNSUPPORTED_MEDIA_TYPE", "Send the body as application/json."),
        _ => ("ERROR", "The request could not be completed.")
    };
    await response.WriteAsJsonAsync(new ApiError(code, message));
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();