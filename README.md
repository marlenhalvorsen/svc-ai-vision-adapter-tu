# svc-ai-vision-adapter

A microservice that listens for events on a Kafka topic containing a presigned URL.  
When triggered, it:

1. Fetches the image from the provided presigned URL  
2. Sends the image to an AI analyzer service (Google Vision, etc.)  
3. Processes and compacts the returned data  
4. Publishes the resulting event to its own Kafka topic  

---

## Features
- Kafka consumer/producer integration  
- Image analysis with AI service  
- Data shaping and compacting  
- Event-driven architecture  

---
