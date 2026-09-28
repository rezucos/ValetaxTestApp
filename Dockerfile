# syntax=docker/dockerfile:1

# One Dockerfile for all services: the solution is restored and published once in the "build" stage,
# every service gets its own small runtime image (docker compose selects it with "target").

# ---------------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore in its own layer: it is repeated only when project files change, not on every code change.
COPY global.json Directory.Build.props Directory.Packages.props ValetaxTest.slnx ./
COPY src/Libraries/Core/Core.csproj src/Libraries/Core/
COPY src/Libraries/EventBus/EventBus.csproj src/Libraries/EventBus/
COPY src/Libraries/MessageLogEF/MessageLogEF.csproj src/Libraries/MessageLogEF/
COPY src/Libraries/EventBusKafka/EventBusKafka.csproj src/Libraries/EventBusKafka/
COPY src/Libraries/PersistencePostgres/PersistencePostgres.csproj src/Libraries/PersistencePostgres/
COPY src/Libraries/ValetaxTest.ServiceDefaults/ValetaxTest.ServiceDefaults.csproj src/Libraries/ValetaxTest.ServiceDefaults/
COPY src/Services/Partners/Partners.Domain/Partners.Domain.csproj src/Services/Partners/Partners.Domain/
COPY src/Services/Partners/Partners.Application/Partners.Application.csproj src/Services/Partners/Partners.Application/
COPY src/Services/Partners/Partners.Infrastructure/Partners.Infrastructure.csproj src/Services/Partners/Partners.Infrastructure/
COPY src/Services/Partners/Partners.API/Partners.API.csproj src/Services/Partners/Partners.API/
COPY src/Services/Events/Events.Domain/Events.Domain.csproj src/Services/Events/Events.Domain/
COPY src/Services/Events/Events.Application/Events.Application.csproj src/Services/Events/Events.Application/
COPY src/Services/Events/Events.Infrastructure/Events.Infrastructure.csproj src/Services/Events/Events.Infrastructure/
COPY src/Services/Events/Events.API/Events.API.csproj src/Services/Events/Events.API/
COPY src/Services/Commissions/Commissions.Domain/Commissions.Domain.csproj src/Services/Commissions/Commissions.Domain/
COPY src/Services/Commissions/Commissions.Application/Commissions.Application.csproj src/Services/Commissions/Commissions.Application/
COPY src/Services/Commissions/Commissions.Infrastructure/Commissions.Infrastructure.csproj src/Services/Commissions/Commissions.Infrastructure/
COPY src/Services/Commissions/Commissions.API/Commissions.API.csproj src/Services/Commissions/Commissions.API/
COPY src/Services/Wallets/Wallets.Domain/Wallets.Domain.csproj src/Services/Wallets/Wallets.Domain/
COPY src/Services/Wallets/Wallets.Application/Wallets.Application.csproj src/Services/Wallets/Wallets.Application/
COPY src/Services/Wallets/Wallets.Infrastructure/Wallets.Infrastructure.csproj src/Services/Wallets/Wallets.Infrastructure/
COPY src/Services/Wallets/Wallets.API/Wallets.API.csproj src/Services/Wallets/Wallets.API/
COPY src/Services/Payouts/Payouts.Domain/Payouts.Domain.csproj src/Services/Payouts/Payouts.Domain/
COPY src/Services/Payouts/Payouts.Application/Payouts.Application.csproj src/Services/Payouts/Payouts.Application/
COPY src/Services/Payouts/Payouts.Infrastructure/Payouts.Infrastructure.csproj src/Services/Payouts/Payouts.Infrastructure/
COPY src/Services/Payouts/Payouts.API/Payouts.API.csproj src/Services/Payouts/Payouts.API/
COPY src/ApiGateway/ApiGateway.csproj src/ApiGateway/
# The solution also lists the test projects, so restore needs their .csproj; the tests are not built into images.
COPY tests/Commissions.UnitTests/Commissions.UnitTests.csproj tests/Commissions.UnitTests/
COPY tests/Payouts.UnitTests/Payouts.UnitTests.csproj tests/Payouts.UnitTests/
RUN dotnet restore ValetaxTest.slnx

COPY src/ src/
RUN set -e; \
    for project in \
        ApiGateway \
        Services/Partners/Partners.API \
        Services/Events/Events.API \
        Services/Commissions/Commissions.API \
        Services/Wallets/Wallets.API \
        Services/Payouts/Payouts.API; \
    do \
        name="$(basename "$project")"; \
        dotnet publish "src/$project/$name.csproj" -c Release --no-restore -o "/app/$name" -p:UseAppHost=false; \
    done

# ---------------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# curl is used by the container health checks (GET /health/ready).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
# Listening addresses come from the Kestrel section of appsettings.json (HTTP/1.1 API + HTTP/2 gRPC ports).
ENV ASPNETCORE_HTTP_PORTS=
# Non-root user shipped with the official .NET images.
USER $APP_UID
EXPOSE 8080

FROM runtime AS gateway
COPY --from=build /app/ApiGateway ./
ENTRYPOINT ["dotnet", "ApiGateway.dll"]

FROM runtime AS partners
COPY --from=build /app/Partners.API ./
EXPOSE 8081
ENTRYPOINT ["dotnet", "Partners.API.dll"]

FROM runtime AS events
COPY --from=build /app/Events.API ./
ENTRYPOINT ["dotnet", "Events.API.dll"]

FROM runtime AS commissions
COPY --from=build /app/Commissions.API ./
EXPOSE 8081
ENTRYPOINT ["dotnet", "Commissions.API.dll"]

FROM runtime AS wallets
COPY --from=build /app/Wallets.API ./
ENTRYPOINT ["dotnet", "Wallets.API.dll"]

FROM runtime AS payouts
COPY --from=build /app/Payouts.API ./
ENTRYPOINT ["dotnet", "Payouts.API.dll"]
