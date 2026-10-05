FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, in its own layer, so code changes don't re-download packages.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Products.Domain/Products.Domain.csproj src/Products.Domain/
COPY src/Products.Application/Products.Application.csproj src/Products.Application/
COPY src/Products.Infrastructure/Products.Infrastructure.csproj src/Products.Infrastructure/
COPY src/Products.Api/Products.Api.csproj src/Products.Api/
RUN dotnet restore src/Products.Api/Products.Api.csproj

COPY src/ src/
RUN dotnet publish src/Products.Api/Products.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
# Run as the unprivileged user that the .NET images provide.
USER app
ENTRYPOINT ["dotnet", "Products.Api.dll"]
