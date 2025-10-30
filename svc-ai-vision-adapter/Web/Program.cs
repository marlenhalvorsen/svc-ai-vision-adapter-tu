using svc_ai_vision_adapter.Application.Ports.In;
using svc_ai_vision_adapter.Application.Ports.Out;
using svc_ai_vision_adapter.Application.Services;
using svc_ai_vision_adapter.Application.Services.Factories;
using svc_ai_vision_adapter.Application.Services.Shaping;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision;
using svc_ai_vision_adapter.Infrastructure.Factories;
using svc_ai_vision_adapter.Infrastructure.Options;
using svc_ai_vision_adapter.Infrastructure.Adapters.BrandCatalog;
using svc_ai_vision_adapter.Application.Services.Aggregation;
using svc_ai_vision_adapter.Infrastructure.Adapters.Http;
using svc_ai_vision_adapter.Application.MessageHandling;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Serialization;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Consumers;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka;
using Confluent.Kafka;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Producers;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.Configure<RecognitionOptions>(builder.Configuration.GetSection("Recognition"));
builder.Services.Configure<KafkaConsumerOptions>(builder.Configuration.GetSection("Kafka:Consumer"));
builder.Services.Configure<KafkaProducerOptions>(builder.Configuration.GetSection("Kafka:Producer"));

//Dependency Injection
builder.Services.AddScoped<IRecognitionService, RecognitionService>();
builder.Services.AddScoped<IRecognitionRequestedHandler, RecognitionRequestedHandler>();
builder.Services.AddTransient<IImageFetcher, HttpImageFetcher>();
builder.Services.AddTransient<GoogleVisionAnalyzer>();
builder.Services.AddSingleton<IAnalyzerFactory, AnalyzerFactory>();
builder.Services.AddCors(p => p.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddSingleton<IResultShaper, GoogleResultShaper>(); 
builder.Services.AddSingleton<IResultShaperFactory, ResultShaperFactory>();
builder.Services.AddSingleton<IResultAggregator, ResultAggregatorService>();
builder.Services.AddSingleton<IBrandCatalog>(sp =>
    new JsonBrandCatalog(Path.Combine(AppContext.BaseDirectory, "Resources", "brands.json")));
builder.Services.AddSingleton<IKafkaSerializer, JsonKafkaSerializer>();
//Kafka consumer as backgroundService
builder.Services.AddHostedService<RecognitionRequestedKafkaConsumer>();
builder.Services.AddSingleton<IRecognitionCompletedPublisher, ReocgnitionCompletedKafkaProducer>(); 



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
