# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY PayFlow.sln .
COPY PayFlow.Domain/PayFlow.Domain.csproj PayFlow.Domain/
COPY PayFlow.Services/PayFlow.Services.csproj PayFlow.Services/
COPY PayFlow.Providers/PayFlow.Providers.csproj PayFlow.Providers/
COPY PayFlow.API/PayFlow.API.csproj PayFlow.API/

# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY PayFlow.Domain/ PayFlow.Domain/
COPY PayFlow.Services/ PayFlow.Services/
COPY PayFlow.Providers/ PayFlow.Providers/
COPY PayFlow.API/ PayFlow.API/

# Build the application
WORKDIR /src/PayFlow.API
RUN dotnet build -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published files
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "PayFlow.API.dll"]

