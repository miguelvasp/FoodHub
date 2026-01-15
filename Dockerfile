FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FoodHub.Orders.Api/FoodHub.Orders.Api.csproj FoodHub.Orders.Api/
COPY FoodHub.Orders.Domain/FoodHub.Orders.Domain.csproj FoodHub.Orders.Domain/
COPY FoodHub.Orders.Data/FoodHub.Orders.Data.csproj FoodHub.Orders.Data/
COPY FoodHub.Orders.slnx ./

RUN dotnet restore FoodHub.Orders.Api/FoodHub.Orders.Api.csproj

COPY . .

RUN dotnet publish FoodHub.Orders.Api/FoodHub.Orders.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FoodHub.Orders.Api.dll"]
