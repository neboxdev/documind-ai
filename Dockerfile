FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY DocuMind.slnx .
COPY nuget.config .
COPY src/DocuMind.Domain/DocuMind.Domain.csproj src/DocuMind.Domain/
COPY src/DocuMind.Application/DocuMind.Application.csproj src/DocuMind.Application/
COPY src/DocuMind.Infrastructure/DocuMind.Infrastructure.csproj src/DocuMind.Infrastructure/
COPY src/DocuMind.API/DocuMind.API.csproj src/DocuMind.API/
RUN dotnet restore src/DocuMind.API/DocuMind.API.csproj

COPY src/ src/
WORKDIR /src/src/DocuMind.API
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# SQLite database gets stored here
VOLUME /app/data
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/documind.db"

ENTRYPOINT ["dotnet", "DocuMind.API.dll"]
