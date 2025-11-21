FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /app

COPY Directory.Build.* ./
COPY src/AzureKeyVaultEmulator/AzureKeyVaultEmulator.csproj ./AzureKeyVaultEmulator/
COPY src/AzureKeyVaultEmulator/packages.lock.json ./AzureKeyVaultEmulator/
RUN dotnet restore AzureKeyVaultEmulator/AzureKeyVaultEmulator.csproj \
        --use-lock-file --locked-mode

ARG SKIP_CODE_GENERATION
ENV SKIP_CODE_GENERATION=${SKIP_CODE_GENERATION:-false}

COPY src .
RUN dotnet publish AzureKeyVaultEmulator/AzureKeyVaultEmulator.csproj \
        -c Release -o publish --no-restore && \
    mkdir -p /app/publish/.vault

########################################

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
ENV ASPNETCORE_URLS=https://+:11001
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/.certs/emulator.pfx
ENTRYPOINT ["dotnet", "AzureKeyVaultEmulator.dll"]
VOLUME ["/app/.vault"]
VOLUME ["/app/.certs"]

COPY --link .certs/emulator.pfx .certs/emulator.pfx
COPY --chown=app:app --chmod=755 --link --from=build /app/publish .
