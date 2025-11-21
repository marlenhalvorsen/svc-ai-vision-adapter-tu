┌─────────────────────────────────────────────────────────────┐
│                    Kafka Consumer Layer                      │
└─────────────────────────────────────────────────────────────┘
            │
            ▼
┌───────────────────────────────┐
│ RecognitionRequestedKafkaCons │  (BackgroundService)
└───────────────────────────────┘
            │ deserializes Kafka message
            ▼
┌───────────────────────────────┐
│  IRecognitionRequestedHandler │
└───────────────────────────────┘
            │
            ▼
┌───────────────────────────────┐
│       RecognitionService      │   (Application Layer)
└───────────────────────────────┘
            │
            ├────────── fetch presigned URLs ───────────────────►
            │                    (IImageUrlFetcher)
            │
            ├────────── fetch bytes per URL ────────────────────►
            │                    (IImageFetcher)
            │
            ├────────── Google Vision analyze ──────────────────►
            │                    (IImageAnalyzer)
            │
            ├────────── shape results ──────────────────────────►
            │                    (IResultShaper)
            │
            ├────────── aggregate shape ─────────────────────────►
            │                    (IResultAggregator)
            │
            └── if EnableReasoning == true:
                   ▼
          ┌───────────────────────────────────┐
          │   IMachineReasoningAnalyzer       │
          │    (GeminiMachineAnalyzer)        │
          └───────────────────────────────────┘
                   │ uses
                   ▼
          ┌───────────────────────────────────┐
          │   GeminiPromptLoader              │
          └───────────────────────────────────┘
                   │ builds prompt
                   ▼
     ┌────────────────────────────────────────────┐
     │ Google Gemini API (generateContent call)   │
     └────────────────────────────────────────────┘
                   │ returns JSON
                   ▼
     ┌────────────────────────────────────────────┐
     │ GeminiToAggregateMapper                     │
     └────────────────────────────────────────────┘
                   │
                   ▼
       back to RecognitionService(Aggregate updated)

            ▼
┌───────────────────────────────┐
│ Rec. Completed Kafka Producer │
│   (IRecognitionCompletedPub)  │
└───────────────────────────────┘
            │ serializes DTO
            ▼
───────────────────────────────────
Kafka Topic: tu.recognition.completed
───────────────────────────────────
