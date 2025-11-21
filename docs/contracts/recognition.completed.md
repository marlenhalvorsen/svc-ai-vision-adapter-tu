# Messaging Contract – RecognitionCompleted (v0)

**Version:** Draft v0 (unfrozen)  
**Last updated:** 2025-11-12  
**Owner:** Trackunit AI Vision Adapter Service  
**Status:** v1 – Frozen (breaking changes require new schema version)

---

## Overview  
Emitted when an AI recognition job has successfully completed.  
Signals that image analysis and aggregation have finished, and the resulting machine classification is ready for downstream consumption (e.g., enrichment, data pipeline).  

---

## Topic and Key  
- **Topic:** `tu.recognition.completed`  
- **Message key:** the first `objectKey` from the recognition request  
  - Ensures ordering for all recognition results referring to the same uploaded image.
  - Mirrors the key strategy used by `tu.images.uploaded` for correlation consistency.
  
---

## Headers  
| Header | Example | Description |
|---------|----------|-------------|
| x-schema | recognition.completed.v1 | Schema version identifier |
| x-producer | svc-ai-vision-adapter | Originating service |
| x-correlation-id | 1f9f7f4c-a8b4-4e21-b0a2-b23fd4e98311 | Propagated correlation ID for traceability |

---

## Payload  

| Field | Type | Description |
|-------|------|-------------|
| provider | [AIProviderDto](../../Application/Contracts/AIProviderDto.cs) | Provider metadata (e.g., name, model, version) |
| aggregate | [MachineAggregateDto](../../Application/Contracts/MachineAggregateDto.cs) | Aggregated recognition result combining evidence and confidence from all analyzed images |

### Example
```json
{
  "provider": {
    "name": "GoogleVision",
    "model": "VisionModel-v3.2",
    "features": [ "LOGO_DETECTION", "WEB_DETECTION" ],
    "region": "us-central1"
  },
"aggregate": {
    "brand": "Komatsu",
    "type": "Excavator",
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

## Semantics
- Represents the successful completion of a recognition process triggered by an `tu.images.uploaded` event.
- Indicates that all configured AI providers have returned results and that aggregation has been performed.
- Downstream systems can use this event to enrich assets, update metadata, or trigger subsequent workflows.

---

## Delivery Semantics
- **Delivery:** at-least-once; duplicates possible.
- **Ordering:** guaranteed per `objectKey`.
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
| 2025-11-18  | v1      | Frozen, stable production version. No breaking changes allowed. |

---

## Related Documents
- [Recognition Pipeline Overview](../../docs/architecture/recognition-pipeline.md)

**End of document**

