using D365MarketingForms.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Xrm.Sdk.Query;
using System.Text;
using Insurgo.Model;
using D365MarketingForms.Server.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Add JWT Authentication definition to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n" +
                      "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                      "Example: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });

    // Make sure all endpoints use this security definition
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register Dataverse service
builder.Services.AddTransient<IDataverseService, DataverseService>();

// Register caching services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// Configure JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
builder.Services.AddCors();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors(builder =>
    builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "D365 Marketing Forms API v1");
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.DefaultModelsExpandDepth(0); // Hide the schemas section
        options.EnableDeepLinking();
        options.DisplayRequestDuration();
    });
}

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/marketingforms", async (IDataverseService dataverseService, ICacheService cacheService) =>
{
    var cacheKey = "marketing_forms";
    var cachedForms = cacheService.Get<MarketingForm[]>(cacheKey);
    if (cachedForms != null)
    {
        return Results.Ok(cachedForms);
    }

    var client = dataverseService.GetClient();
    var query = new QueryExpression(msdynmkt_marketingform.EntityLogicalName)
    {
        ColumnSet = new ColumnSet(msdynmkt_marketingform.Fields.msdynmkt_name, msdynmkt_marketingform.Fields.msdynmkt_standalonehtml),
        NoLock = true
    };
    query.Criteria.AddCondition(msdynmkt_marketingform.Fields.statuscode, ConditionOperator.Equal, (int)msdynmkt_marketingform_statuscode.Live);
    query.Criteria.AddCondition(msdynmkt_marketingform.Fields.msdynmkt_standalonehtml, ConditionOperator.NotNull);
    // Will be change later once we support other form types
    query.Criteria.AddCondition(msdynmkt_marketingform.Fields.msdynmkt_marketingformtype, ConditionOperator.Equal, (int)msdynmkt_marketingformtype.Marketingform);

    var forms = client.RetrieveMultiple(query).Entities
        .Select(e => e.ToEntity<msdynmkt_marketingform>())
        .Select(e => new MarketingForm(
            e.msdynmkt_name ?? "",
            e.msdynmkt_standalonehtml ?? ""))
        .ToArray();

    // Cache the results for 15 minutes
    cacheService.Set(cacheKey, forms, TimeSpan.FromMinutes(15));

    return Results.Ok(forms);
})
.RequireAuthorization()
.WithName("GetMarketingForms")
.WithOpenApi();

app.MapGet("/marketingforms/{idOrSlug}", async (string idOrSlug, IDataverseService dataverseService, ICacheService cacheService) => 
{
    // Check if the parameter is a valid GUID (ID) or a slug
    bool isGuid = Guid.TryParse(idOrSlug, out Guid formId);

    // Create a cache key based on what we're looking for
    var cacheKey = isGuid
        ? $"marketing_form_id_{formId}"
        : $"marketing_form_slug_{idOrSlug}";

    // Try to get from cache
    var cachedForm = cacheService.Get<MarketingForm>(cacheKey);
    if (cachedForm != null)
    {
        return Results.Ok(cachedForm);
    }

    // Not in cache, need to query Dataverse
    var client = dataverseService.GetClient();

    QueryExpression query = new(msdynmkt_marketingform.EntityLogicalName)
    {
        ColumnSet = new ColumnSet(
            msdynmkt_marketingform.Fields.msdynmkt_name,
            msdynmkt_marketingform.Fields.msdynmkt_standalonehtml),
        NoLock = true
    };

    // Add the appropriate filter based on whether we're searching by ID or slug
    if (isGuid)
    {
        query.Criteria.AddCondition(msdynmkt_marketingform.Fields.Id, ConditionOperator.Equal, formId);
    }
    else
    {
        var name = SlugUtility.DeSlug(idOrSlug);
        query.Criteria.AddCondition(msdynmkt_marketingform.Fields.msdynmkt_name, ConditionOperator.Equal, name);
    }

    // Only return live forms
    query.Criteria.AddCondition(msdynmkt_marketingform.Fields.statuscode, ConditionOperator.Equal, (int)msdynmkt_marketingform_statuscode.Live);

    var result = client.RetrieveMultiple(query);

    if (result.Entities.Count == 0)
    {
        return Results.NotFound($"Marketing form with {(isGuid ? "ID" : "slug")} '{idOrSlug}' not found");
    }

    var formEntity = result.Entities[0].ToEntity<msdynmkt_marketingform>();

    var form = new MarketingForm(
        formEntity.msdynmkt_name ?? "",
        formEntity.msdynmkt_standalonehtml ?? "");

    // Cache the result for 15 minutes
    cacheService.Set(cacheKey, form, TimeSpan.FromMinutes(15));

    return Results.Ok(form);
})
.RequireAuthorization()
.WithName("GetMarketingFormByIdOrSlug")
.WithOpenApi();

// Replace the token endpoint and related code
app.MapPost("/token", (ApiKeyRequest request) =>
{
    // Validate API key instead of username/password
    if (!IsValidApiKey(request.ApiKey)) return Results.Unauthorized();

    var token = GenerateJwtToken(request.ApiKey);
    return Results.Ok(new { token });
})
.WithName("GetToken")
.WithOpenApi();

app.MapFallbackToFile("/index.html");

app.Run();

// Token generation method - updated to use API key instead of username
string GenerateJwtToken(string apiKey)
{
    var issuer = app.Configuration["Jwt:Issuer"];
    var audience = app.Configuration["Jwt:Audience"];
    var key = Encoding.UTF8.GetBytes(app.Configuration["Jwt:Key"]);
    var signingCredentials = new SigningCredentials(
        new SymmetricSecurityKey(key),
        SecurityAlgorithms.HmacSha256Signature
    );

    var subject = new System.Security.Claims.ClaimsIdentity(new[]
    {
        new System.Security.Claims.Claim("api_key", apiKey),
        // You can add standard claims if needed
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "api_client"),
    });

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = subject,
        Expires = DateTime.UtcNow.AddDays(30), // Longer expiration for API tokens
        Issuer = issuer,
        Audience = audience,
        SigningCredentials = signingCredentials
    };

    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}

// Method to validate API key
bool IsValidApiKey(string apiKey)
{
    // Retrieve valid API keys from configuration
    var validApiKeys = app.Configuration.GetSection("ApiKeys").Get<string[]>() ?? Array.Empty<string>();

    // Option 1: Simple comparison (for development/testing)
    return validApiKeys.Contains(apiKey);

    // Option 2: For production, consider implementing a more secure validation
    // such as time-based comparison to prevent timing attacks
    // return CryptographicOperations.FixedTimeEquals(
    //    Encoding.UTF8.GetBytes(apiKey),
    //    Encoding.UTF8.GetBytes(expectedApiKey));
}

internal record MarketingForm(string Name, string HtmlContent)
{
    public string Slug => SlugUtility.GenerateSlug(Name);
}

// Replace TokenRequest with ApiKeyRequest
internal record ApiKeyRequest(string ApiKey);
