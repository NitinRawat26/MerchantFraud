# MerchantFraud API and workbench. Build: docker build -t merchantfraud-api .
# Run:   docker run -p 8080:8080 -v merchantfraud-data:/var/data merchantfraud-api
FROM node:24-bookworm-slim AS web
WORKDIR /src/web/workbench
ENV NG_CLI_ANALYTICS=false
COPY web/workbench/package.json web/workbench/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY web/workbench/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ src/
RUN dotnet publish src/MerchantFraud.Api/MerchantFraud.Api.csproj -c Release -o /app --nologo

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
COPY --from=web /src/web/workbench/dist/workbench/browser ./wwwroot
RUN mkdir -p /var/data
ENV ASPNETCORE_ENVIRONMENT=Production \
    PORT=8080
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet MerchantFraud.Api.dll --urls http://0.0.0.0:${PORT}"]
