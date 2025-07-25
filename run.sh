#!/bin/bash

BASE_DIR=$(realpath "$(dirname "$0")")

dotnet run --project "$BASE_DIR/src/AzureKeyVaultEmulator"
