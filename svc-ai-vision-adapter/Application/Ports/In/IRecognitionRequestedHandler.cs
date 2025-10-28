using System.Threading;
using System.Threading.Tasks;
using svc_ai_vision_adapter.Application.Contracts;

// This interface is an "input port" for the application layer.
// It represents an operation the application can perform
// when a new recognition request comes in from the outside world (Kafka).
// Infrastructure (Kafka consumer) will call this interface,
// but the interface itself lives in Application so Application stays in control.
namespace svc_ai_vision_adapter.Application.Ports.In
{
    public interface IRecognitionRequestedHandler
    {
        //Defines how the application wants to recieve work.
        Task HandleAsync(RecognitionRequestDto request, CancellationToken ct); 
    }
}
