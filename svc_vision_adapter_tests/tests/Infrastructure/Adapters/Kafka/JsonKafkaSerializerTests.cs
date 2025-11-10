using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Serialization;
using svc_ai_vision_adapter.Application.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace svc_ai_vision_adapter_tests;

[TestClass]
public class JsonKafkaSerializerTests
{
    [TestMethod]
    public void SerializerRountrip_ReturnsSameObject()
    {
        //ARRANGE
        var serializer = new JsonKafkaSerializer();
        var evt = new RecognitionCompletedEvent("123", new AIProviderDto 
        ( 
            "AI",
            null,
            null,
            Array.Empty<string>(),
            null
        ), 
        new MachineAggregateDto
        {
            Brand = "Siemens",
            Type = "CNC-Mill",
            Model = "X200",
            Confidence = 0.92,
            IsConfident = true,
            TypeConfidence = 0.89,
            TypeSource = "Google-Vision"
        });

        //ACT
        var bytes = serializer.Serialize(evt);
        var deserialized = serializer.Deserialize<RecognitionCompletedEvent>(bytes);
         
        //ASSERT
        Assert.AreEqual(evt.SessionId, deserialized.SessionId);
        //testing nested object values string, double and bool
        Assert.AreEqual(evt.Aggregate.Name, deserialized.Aggregate.Name);
        Assert.AreEqual(evt.Aggregate.IsConfident, deserialized.Aggregate.IsConfident);
        Assert.AreEqual(evt.Aggregate.Confidence, deserialized.Aggregate.Confidence);
        Assert.AreEqual(evt.Provider.Name, deserialized.Provider.Name);

    }
}
