using EPaymentMiddleware.Data;
using EPaymentMiddleware.Models.Satim;
using EPaymentMiddleware.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure SatimSettings from appsettings.json
builder.Services.Configure<SatimSettings>(builder.Configuration.GetSection("SatimSettings"));

// Register HttpClient for SATIM calls
builder.Services.AddHttpClient("Satim", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register the database context (SQL Server)
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the SATIM payment service
builder.Services.AddScoped<ISatimPaymentService, SatimPaymentService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — allow any calling application to consume the middleware
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
