# Trackunit AI Vision Adapter  
![CI](https://github.com/Team-2-Devs/svc-ai-vision-adapter/actions/workflows/ci.yml/badge.svg?branch=Development)

## Status  
**Active development** – PoC phase for AI-driven machine recognition.  

---

## Purpose  
AI Vision Adapter is a microservice responsible for analyzing uploaded images using external AI providers (e.g., Google Vision and later Gemini).  
It consumes `tu.images.uploaded` events, performs image recognition, and publishes `tu.recognition.completed` results for downstream services.  

---

## Responsibilities  
- Consume `ImageUploaded` events from Kafka (`tu.images.uploaded`)  
- Fetch presigned URLs for uploaded images from `tu-media-access-service`  
- Analyze images via AI providers  
- Shape, aggregate, and enrich AI output  
- Publish structured results as `RecognitionCompleted` events (`tu.recognition.completed`)  

---

## Architecture  
**Pattern:** Hexagonal (Ports and Adapters)  

| Layer | Description |
|--------|-------------|
| **Application** | Core orchestration and business rules (recognition flow, shaping, aggregation) |
| **Infrastructure** | Adapters for Kafka, HTTP (presigned URL), and AI provider integration |
| **Domain Contracts** | Shared DTOs and message models used across adapters |
| **Tests** | Unit and integration tests verifying message handling and external communication |

**Tech stack:**  
- .NET 8  
- Confluent.Kafka  
- Microsoft.Extensions stack (DI, Logging, Options)  
- MSTest + Moq for unit tests  

---

## Messaging  
| Direction | Topic | Contract | Description |
|------------|--------|-----------|-------------|
| **Consume** | `tu.images.uploaded` | Defined by Trackunit Ingestion Service (`ImageUploaded v0`) | Triggered when image upload is confirmed stored |
| **Produce** | `tu.recognition.completed` | [RecognitionCompleted (v0)](./docs/contracts/recognition.completed.md) | Published when AI analysis is complete |

---

## Local Development  
```bash
dotnet run --project svc_ai_vision_adapter
```
---

## CI / Test Coverage  
- Build & test via reusable workflow: `Team-2-Devs/.github/.github/workflows/dotnet-ci.yml`  
- Test coverage published via `coverlet` + `reportgenerator`  

---

## Related Services  
- **tu-ingestion-service** – Publishes `ImageUploaded` events  
- **tu-storage-service** – Provides object storage and presigned URLs  
- **tu-media-access-service** – Grants secure GET access for AI analysis  

---

## Documentation  
 **Messaging Contract:**  
- [RecognitionCompleted (v0)](./docs/contracts/recognition.completed.md)  

