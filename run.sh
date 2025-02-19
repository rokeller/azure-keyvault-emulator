#!/bin/bash

BASE_DIR=$(realpath $(dirname $0))

ASPNETCORE_Kestrel__Certificates__Default__Path=$BASE_DIR/.certs/emulator.pfx \
    dotnet run --project $BASE_DIR/AzureKeyVaultEmulator
