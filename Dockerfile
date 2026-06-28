# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/ComplianceGuardian.Api/ComplianceGuardian.Api.csproj ./ComplianceGuardian.Api/
RUN dotnet restore ./ComplianceGuardian.Api/ComplianceGuardian.Api.csproj

COPY src/ComplianceGuardian.Api/ ./ComplianceGuardian.Api/
RUN dotnet publish ./ComplianceGuardian.Api/ComplianceGuardian.Api.csproj \
    -c Release -o /app --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ComplianceGuardian.Api.dll"]
