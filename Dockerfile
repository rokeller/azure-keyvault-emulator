FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
WORKDIR /app

COPY AzureKeyVaultEmulator/AzureKeyVaultEmulator.csproj ./AzureKeyVaultEmulator/
COPY AzureKeyVaultEmulator/packages.lock.json ./AzureKeyVaultEmulator/
RUN dotnet restore AzureKeyVaultEmulator/AzureKeyVaultEmulator.csproj \
        --use-lock-file --locked-mode

COPY . .
RUN dotnet publish AzureKeyVaultEmulator/AzureKeyVaultEmulator.csproj \
        -c Release -o publish --no-restore && \
    rm /app/publish/packages.lock.json && \
    mkdir -p /app/publish/.vault

########################################

FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled
WORKDIR /app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
ENV ASPNETCORE_URLS=https://+:11001
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/.certs/emulator.pfx
ENTRYPOINT ["dotnet", "AzureKeyVaultEmulator.dll"]
VOLUME ["/app/.vault"]
VOLUME ["/app/.certs"]

COPY .certs/emulator.pfx .certs/emulator.pfx
COPY --chown=app:app --chmod=755 --link --from=build /app/publish .
