FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
WORKDIR /app

COPY AzureKeyVaultEmulator/AzureKeyVaultEmulator.csproj ./AzureKeyVaultEmulator/
RUN dotnet restore AzureKeyVaultEmulator/AzureKeyVaultEmulator.csproj --use-lock-file --locked-mode

COPY . .
RUN dotnet publish AzureKeyVaultEmulator/AzureKeyVaultEmulator.csproj -c Release -o publish --no-restore

########################################

FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled
WORKDIR /app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
ENTRYPOINT ["dotnet", "AzureKeyVaultEmulator.dll"]
VOLUME ["/app/.vault"]

COPY --link --from=build /app/publish .
