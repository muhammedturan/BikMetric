FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["BikMetric.Api/BikMetric.Api.csproj", "BikMetric.Api/"]
COPY ["BikMetric.Application/BikMetric.Application.csproj", "BikMetric.Application/"]
COPY ["BikMetric.Infrastructure/BikMetric.Infrastructure.csproj", "BikMetric.Infrastructure/"]
COPY ["BikMetric.Domain/BikMetric.Domain.csproj", "BikMetric.Domain/"]
COPY ["BikMetric.Core/BikMetric.Core.csproj", "BikMetric.Core/"]
RUN dotnet restore "BikMetric.Api/BikMetric.Api.csproj"
COPY . .
WORKDIR "/src/BikMetric.Api"
RUN dotnet build "BikMetric.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BikMetric.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BikMetric.Api.dll"]
