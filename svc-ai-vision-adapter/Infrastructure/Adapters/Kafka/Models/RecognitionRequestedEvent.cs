using System.Collections.Generic; 

///Representation of the shape of the message that is published
///on the Kfka topic. Therefore the "wire contract" /integration contract
namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models
{
    internal sealed class RecognitionRequestedEvent
    {
        public string RequestID { get; set; } = default!; 
        public string Provider {  get; set; } = default!;
        public List<string> ImageUrls { get; set; } = new(); 
    }
}
