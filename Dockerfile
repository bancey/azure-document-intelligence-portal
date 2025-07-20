# See https://docs.docker.com/engine/reference/builder/
# Use the official .NET 8 SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY src/DocumentIntelligencePortal/DocumentIntelligencePortal.csproj ./
RUN dotnet restore DocumentIntelligencePortal.csproj

# Copy the rest of the source code
COPY src/DocumentIntelligencePortal/ ./

# Build and publish the app
RUN dotnet publish DocumentIntelligencePortal.csproj -c Release -o /app/publish --no-restore

# Use the official .NET 8 ASP.NET runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 80
EXPOSE 80

# Set the entrypoint
ENTRYPOINT ["dotnet", "DocumentIntelligencePortal.dll"]
