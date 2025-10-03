# Svc AI Vision Adapter

![CI](https://github.com/Team-2-Devs/svc-ai-vision-adapter/actions/workflows/ci.yml/badge.svg?branch=Development)

## Status
Under development

## Overview
A proof of concept (PoC) for applying AI to identify and categorize machines.

This microservice listens for events on a Kafka topic containing a presigned URL.  
When triggered, it:

1. Fetches the image from the provided presigned URL  
2. Sends the image to an AI analyzer service (e.g., Google Vision)  
3. Processes and compacts the returned data  
4. Publishes the resulting event to its own Kafka topic  

## Features
- Kafka consumer/producer integration  
- Image analysis with AI service  
- Data shaping and compacting  
- Event-driven architecture  
- Hexagonal architecture (modular adapters with separated business logic)
