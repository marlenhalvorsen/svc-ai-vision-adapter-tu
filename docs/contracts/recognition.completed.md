# Messaging Contract – RecognitionCompleted (v1)

**Version:** v1 (frozen)  
**Last updated:** 2025-11-21  
**Owner:** Trackunit AI Vision Adapter Service  
**Status:** v1 – Frozen (breaking changes require new schema version v2)

---

## Overview  
Emitted when an AI recognition job has successfully completed, including optional LLM-based reasoning.  
Downstream consumers use this event to enrich assets, perform metadata processing or trigger workflows.   

---

## Topic and Key  
- **Topic:** `tu.recognition.completed`  
- **Message key:** the first `objectKey` from the recognition request  
  - Ensures per-object ordering and aligns with the ingestion pipeline.  
  - Mirrors the key strategy used by `tu.images.uploaded` for correlation consistency.
  
---

## Headers  
| Header | Example | Description |
|---------|----------|-------------|
| x-schema | recognition.completed.v1 | Schema version identifier |
| x-producer | svc-ai-vision-adapter | Originating service |
| x-correlation-id | 1f9f7f4c-a8b4-4e21-b0a2-b23fd4e98311 | Traceability |

---

## Payload  

| Field | Type | Description |
|-------|------|-------------|
| provider | [AIProviderDto](../../Application/Contracts/AIProviderDto.cs) | Provider metadata for the AI pipeline (Vision + optional reasoning) |
| aggregate | [MachineAggregateDto](../../Application/Contracts/MachineAggregateDto.cs) | Final machine classification |

### Example
```json
{
  "provider": {
    "name": "GoogleVision",
    "model": "v1",
    "features": ["LOGO_DETECTION", "WEB_DETECTION"],
    "region": "us-central1",
    "reasoningName": "gemini",
    "reasoningModel": "gemini-1.5-flash"
  },
  "aggregate": {
    "brand": "Komatsu",
    "machineType": "Excavator",
    "model": "PC200-6",
    "weight": 20500,
    "year": "1994–2002",
    "attachment": ["bucket"],
    "confidence": 0.92,
    "isConfident": true,
    "typeSource": "gemini"
  }
}

```
---

## Provider Semantics (v1)
The provider object represents the entire AI pipeline used for classification.

Vision Stage

The initial provider always refers to Google Vision, which performed low-level image analysis (logo detection, OCR, label detection, etc.).

Reasoning Stage (optional)

If reasoning is enabled, the system also enriches the machine classification with an LLM (e.g., Gemini).
In this case, the provider metadata is extended with:

reasoningName – the LLM provider (e.g., "gemini")

reasoningModel – the specific LLM model used (e.g., "gemini-1.5-pro")

This allows traceabilitu of which LLM was used to enrich the classification. 

---

## Semantics
- Represents the successful completion of a recognition process triggered by an `tu.images.uploaded` event.
- Indicates that all configured AI providers have returned results and that aggregation has been performed.
- Downstream systems can use this event to enrich assets, update metadata, or trigger subsequent workflows.

---

## Delivery Semantics
- **Delivery:** at-least-once; duplicates possible.
- **Ordering:** per `objectKey`.
- **Consumer rule:** deduplicate by combination of `correlationId` + `objectKey`.

---

## Versioning Policy
- v0: freely changeable during development.
- v1 (frozen): once stabilized, breaking changes require new schema (`recognition.completed.v2`).
- Consumers should check `x-schema` for version compatibility.

---

## Changelog
| Date        | Version | Notes |
|-------------|---------|-------|
| 2025-11-12  | v0      | Initial draft version |
| 2025-11-17  | v0      | Updated example payload to match Gemini pipeline |
| 2025-11-18  | v0      | Added Gemini reasoning details |
| 2025-11-18  | v1      | Frozen stable version. |


---

## Related Documents
- [Recognition Pipeline Overview](../../docs/architecture/recognition-pipeline.md)

**End of document**

