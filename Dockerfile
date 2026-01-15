FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FoodHub.Orders.slnx ./
COPY FoodHub.Orders.Api/FoodHub.Orders.Api.csproj FoodHub.Orders.Api/
COPY FoodHub.Orders.Domain/FoodHub.Orders.Domain.csproj FoodHub.Orders.Domain/
COPY FoodHub.Orders.Data/FoodHub.Orders.Data.csproj FoodHub.Orders.Data/
COPY FoodHub.Orders.Tests/FoodHub.Orders.Tests.csproj FoodHub.Orders.Tests/

RUN dotnet restore FoodHub.Orders.Api/FoodHub.Orders.Api.csproj

COPY . .
WORKDIR /src/FoodHub.Orders.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FoodHub.Orders.Api.dll"]
