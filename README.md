# FoodHub Orders API

API REST para gestao de pedidos com MongoDB, seguindo DDD e Clean Architecture.

## Requisitos
- .NET SDK 10
- Docker + Docker Compose

## Como rodar localmente
1) Build
```
dotnet build
```

2) Testes
```
dotnet test
```

3) Subir API + MongoDB
```
docker compose up --build
```

## Endpoints
- Swagger: http://localhost:8080/swagger
- Health: http://localhost:8080/health

## Variaveis de ambiente (Docker)
- `Mongo__ConnectionString` (ex.: `mongodb://mongodb:27017`)
- `Mongo__Database` (ex.: `foodhub_orders`)

## Checklist (Definition of Done)
- CRUD completo de pedidos com MongoDB
- Concorrencia otimista via `Version` com 409
- ProblemDetails para erros consistentes
- Swagger habilitado
- Testes de dominio com xUnit + FluentAssertions + Moq
