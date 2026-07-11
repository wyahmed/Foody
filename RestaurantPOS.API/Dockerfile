FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["RestaurantPOS.API/RestaurantPOS.API.csproj", "RestaurantPOS.API/"]
COPY ["RestaurantPOS.Application/RestaurantPOS.Application.csproj", "RestaurantPOS.Application/"]
COPY ["RestaurantPOS.Domain/RestaurantPOS.Domain.csproj", "RestaurantPOS.Domain/"]
COPY ["RestaurantPOS.Infrastructure/RestaurantPOS.Infrastructure.csproj", "RestaurantPOS.Infrastructure/"]
COPY ["RestaurantPOS.Shared/RestaurantPOS.Shared.csproj", "RestaurantPOS.Shared/"]

RUN dotnet restore "RestaurantPOS.API/RestaurantPOS.API.csproj"

COPY . .
WORKDIR "/src/RestaurantPOS.API"
RUN dotnet build "RestaurantPOS.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RestaurantPOS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RestaurantPOS.API.dll"]
