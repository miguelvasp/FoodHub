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

## Exemplo de payload (Create Order)
```
{
  "code": "ORD-001",
  "orderedAt": "2026-01-14T10:00:00Z",
  "customer": {
    "id": "cust-1",
    "name": "Alice"
  },
  "restaurant": {
    "id": "rest-1",
    "name": "Main Street"
  },
  "items": [
    {
      "product": {
        "id": "prod-1",
        "description": "Burger"
      },
      "quantity": 2,
      "unitPrice": 10.0,
      "notes": "No onions",
      "addonsValue": 1.5
    }
  ],
  "deliveryFee": 5.0,
  "couponCode": "OFF10",
  "orderType": "Delivery"
}
```

## Observacoes sobre Version
- Atualizacoes (PUT) exigem o campo `version`.
- Conflitos de concorrencia retornam HTTP 409.

## Variaveis de ambiente (Docker)
- `ASPNETCORE_ENVIRONMENT=Development` (habilita Swagger)
- `Mongo__ConnectionString` (ex.: `mongodb://mongodb:27017`)
- `Mongo__Database` (ex.: `foodhub_orders`)
- `HttpsRedirection__Enabled=false` (desativa redirecionamento HTTPS no container)

## Checklist (Definition of Done)
- CRUD completo de pedidos com MongoDB
- Concorrencia otimista via `Version` com 409
- ProblemDetails para erros consistentes
- Swagger habilitado
- Testes de dominio com xUnit + FluentAssertions + Moq
