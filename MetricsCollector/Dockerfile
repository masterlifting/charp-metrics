# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["MetricsCollector/MetricsCollector.csproj", "MetricsCollector/"]
RUN dotnet restore "MetricsCollector/MetricsCollector.csproj"
COPY . .
WORKDIR "/src/MetricsCollector"
RUN dotnet publish "MetricsCollector.csproj" -c Release -o /app/publish

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MetricsCollector.dll"]