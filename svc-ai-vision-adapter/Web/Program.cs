using svc_ai_vision_adapter.Application.Ports.Inbound;
using svc_ai_vision_adapter.Application.Ports.Outbound;
using svc_ai_vision_adapter.Application.Services;
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
using Confluent.Kafka;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Producers;
using Microsoft.Extensions.Options;
using Google.Api;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini.Prompt;



var builder = WebApplication.CreateBuilder(args);

//load .env
DotNetEnv.Env.Load();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<RecognitionOptions>(builder.Configuration.GetSection("Recognition"));
builder.Services.Configure<KafkaConsumerOptions>(builder.Configuration.GetSection("Kafka:Consumer"));
builder.Services.Configure<KafkaProducerOptions>(builder.Configuration.GetSection("Kafka:Producer"));
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));

//Dependency Injection
//Kafka consumer as backgroundService
builder.Services.AddSingleton<IConsumer<string, byte[]>>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<KafkaConsumerOptions>>().Value;

    var config = new ConsumerConfig
    {
        BootstrapServers = opts.BootstrapServers,
        GroupId = opts.GroupId,
        EnableAutoCommit = opts.EnableAutoCommit,
        AutoOffsetReset = AutoOffsetReset.Earliest
    };

    return new ConsumerBuilder<string, byte[]>(config)
        .SetKeyDeserializer(Deserializers.Utf8)
        .SetValueDeserializer(Deserializers.ByteArray)
        .Build();
});
builder.Services.AddHostedService<RecognitionRequestedKafkaConsumer>();
builder.Services.AddScoped<IRecognitionService, RecognitionService>();
builder.Services.AddScoped<IRecognitionRequestedHandler, RecognitionRequestedHandler>();
builder.Services.AddHttpClient<IImageUrlFetcher, HttpImageUrlFetcher>((sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(cfg["MediaAccess:BaseUrl"]!);
});

builder.Services.AddTransient<IImageFetcher, HttpImageFetcher>();
builder.Services.AddScoped<IImageAnalyzer, GoogleVisionAnalyzer>();
builder.Services.AddHttpClient<GeminiMachineAnalyzer>();
builder.Services.AddSingleton<IMachineReasoningAnalyzer, GeminiMachineAnalyzer>();
builder.Services.AddSingleton<IPromptLoader, GeminiPromptLoader>();
builder.Services.AddCors(p => p.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddSingleton<IResultShaperFactory, ResultShaperFactory>();
builder.Services.AddSingleton<IResultShaper, GoogleResultShaper>();
builder.Services.AddSingleton<IBrandCatalog>(sp =>
    new JsonBrandCatalog(Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Resources", "brands.json")));
builder.Services.AddSingleton<IResultAggregator, ResultAggregatorService>();
builder.Services.AddSingleton<IKafkaSerializer, JsonKafkaSerializer>();
builder.Services.AddSingleton<IRecognitionCompletedPublisher, RecognitionCompletedKafkaProducer>();
builder.Services.AddSingleton<IProducer<string, byte[]>>(sp =>
{
    var options = sp.GetRequiredService<IOptions<KafkaProducerOptions>>().Value;
    var config = new ProducerConfig
    {
        BootstrapServers = options.BootstrapServers,
        Acks = options.Acks,
        MessageSendMaxRetries = options.MessageSendMaxRetries
    };
    return new ProducerBuilder<string, byte[]>(config)
        .SetKeySerializer(Serializers.Utf8)
        .SetValueSerializer(Serializers.ByteArray)
        .Build();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors();
app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.Run();
