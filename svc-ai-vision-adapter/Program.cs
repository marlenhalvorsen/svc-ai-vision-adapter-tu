using svc_ai_vision_adapter.Application.Interfaces;
using svc_ai_vision_adapter.Application.Services;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision;
using svc_ai_vision_adapter.Infrastructure.Composition;
using svc_ai_vision_adapter.Infrastructure.Factories;
using svc_ai_vision_adapter.Infrastructure.Http;
using svc_ai_vision_adapter.Infrastructure.Options;
using svc_ai_vision_adapter.Infrastructure.Factories;




var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.Configure<RecognitionOptions>(builder.Configuration.GetSection("Recognition"));

//Dependency Injection
builder.Services.AddScoped<IRecognitionService, RecognitionService>();
builder.Services.AddTransient<IImageFetcher, HttpImageFetcher>();
builder.Services.AddTransient<GoogleVisionAnalyzer>();
builder.Services.AddSingleton<IAnalyzerFactory, AnalyzerFactory>();
builder.Services.AddCors(p => p.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddSingleton<IResultShaper, GoogleResultShaper>(); 
builder.Services.AddSingleton<IResultShaperFactory, ResultShaperFactory>();
builder.Services.AddSingleton<IResultAggregator, ResultAggregator>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

//app.UseAuthorization();

app.MapControllers();

app.Run();
