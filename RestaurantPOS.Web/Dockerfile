FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY ["RestaurantPOS.Web/RestaurantPOS.Web.csproj", "RestaurantPOS.Web/"]
COPY ["RestaurantPOS.Application/RestaurantPOS.Application.csproj", "RestaurantPOS.Application/"]
COPY ["RestaurantPOS.Domain/RestaurantPOS.Domain.csproj", "RestaurantPOS.Domain/"]
COPY ["RestaurantPOS.Infrastructure/RestaurantPOS.Infrastructure.csproj", "RestaurantPOS.Infrastructure/"]
COPY ["RestaurantPOS.Shared/RestaurantPOS.Shared.csproj", "RestaurantPOS.Shared/"]

RUN dotnet restore "RestaurantPOS.Web/RestaurantPOS.Web.csproj"

COPY . .
WORKDIR "/src/RestaurantPOS.Web"
RUN dotnet build "RestaurantPOS.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RestaurantPOS.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RestaurantPOS.Web.dll"]
