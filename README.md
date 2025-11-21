# Trackunit AI Vision Adapter
![CI](https://github.com/Team-2-Devs/svc-ai-vision-adapter/actions/workflows/ci.yml/badge.svg?branch=Development)

## Status
Active development – PoC phase for AI-driven machine recognition.

---

## Purpose
AI Vision Adapter is a microservice responsible for analyzing uploaded images using external AI providers such as Google Vision and Gemini.  
It consumes `tu.images.uploaded` events, performs image recognition, optionally enriches the result using an LLM, and publishes a `tu.recognition.completed` event.

---

## Responsibilities
- Consume `ImageUploaded` events from Kafka (`tu.images.uploaded`)
- Fetch presigned URLs for uploaded images from `tu-media-access-service`
- Analyze images using Google Vision and optionally Gemini reasoning
- Shape and aggregate recognition output
- Publish `RecognitionCompleted` events (`tu.recognition.completed`)

---

## Architecture
Pattern: **Hexagonal Architecture (Ports & Adapters)**

### Layer Overview
| Layer | Description |
|-------|-------------|
| **Application** | Orchestration logic, shaping, aggregation, optional reasoning |
| **Infrastructure** | AI adapters (Vision, Gemini), Kafka adapters, HTTP clients |
| **Domain Contracts** | DTOs and message models shared across layers |
| **Tests** | Unit tests for mapping, parsing, adapters, orchestrator |

---

### Technologies
- .NET 8
- Confluent.Kafka
- Google Vision API
- Google Gemini (structured JSON outputs)
- Microsoft.Extensions.* (DI, Options, Logging)
- MSTest + Moq

---

## Messaging Integration

### Consumes
| Topic | Contract | Description |
|--------|----------|-------------|
| `tu.images.uploaded` | ImageUploaded v0 | Triggered when an image is stored in object storage |

### Produces
| Topic | Contract | Description |
|--------|----------|-------------|
| `tu.recognition.completed` | [RecognitionCompleted v1](./docs/contracts/recognition.completed.md) | Published when classification (and optional LLM reasoning) completes |

---

## CI / Test Coverage
- Build & test via reusable workflow: `Team-2-Devs/.github/.github/workflows/dotnet-ci.yml`
- Test coverage generated using Coverlet and ReportGenerator
- All tests run via GitHub Actions on push and PR to `main`

---

## Related Services
- **tu-ingestion-service** – Publishes `ImageUploaded` events  
- **tu-storage-service** – Stores raw images and manages object keys  
- **tu-media-access-service** – Provides presigned URLs for image read-access  
- **Analytics / Metadata downstream services** – Consume `tu.recognition.completed`

---

## Documentation
- **Messaging Contract:**  
  - [RecognitionCompleted v1](./docs/contracts/recognition.completed.md)
- **Architecture:**  
  - Recognition pipeline (Vision → Aggregation → optional Gemini reasoning)  
  - Ports & Adapters overview  
  - DTO and mapping structure

---

## Local Development

This service runs independently and communicates with external systems
(Ingestion, Media Access, Object Storage, Kafka).  
For local development, only the service itself needs to be executed.

Ensure the following environment variables are available:

- `GEMINI_API_KEY` – API key used for LLM reasoning (optional, only required if EnableReasoning = true)
- `Recognition:EnableReasoning` – enables Gemini enrichment (true/false)
- `Recognition:Features` – Vision AI features to request

To run the service locally:

```bash
dotnet run --project svc-ai-vision-adapter
```

If external dependencies (Kafka, presigned URLs, etc.) are not available,
the service can still start, but event processing will not occur.

The service is typically run inside PoC’s development environment and does not require a full local Kafka setup.

## Configuration

| Setting | Description |
|--------|-------------|
| Recognition:Features | Vision features to use |
| Recognition:EnableReasoning | Enables Gemini enrichment |
| Gemini:Model | LLM model to call |
| Gemini:SchemaPath | Path to JSON schema |

---
