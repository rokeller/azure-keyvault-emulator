#!/bin/bash

set -e

if ! command -v pnpm > /dev/null ; then
    echo 'pnpm not found. Please install from https://pnpm.io/'
    exit 1
fi

BASE_PATH=$(dirname $0)
pnpm -C $BASE_PATH install

API_VERSION=${1:-7.4}
BASE_URL='https://raw.githubusercontent.com/Azure/azure-rest-api-specs/refs/heads/main/specification/keyvault/data-plane/Microsoft.KeyVault/stable'
URL_SPEC_COMMON="$BASE_URL/$API_VERSION/common.json"
URL_SPEC_KEYS="$BASE_URL/$API_VERSION/keys.json"
URL_SPEC_SECRETS="$BASE_URL/$API_VERSION/secrets.json"

TARGET_DIR=$BASE_PATH/v$API_VERSION

FILE_SPEC_COMMON="$TARGET_DIR/common.json"  # Keep this as JSON
FILE_SPEC_KEYS="$TARGET_DIR/keys.yaml"
FILE_SPEC_SECRETS="$TARGET_DIR/secrets.yaml"


mkdir -p $TARGET_DIR || true

if [ ! -f $FILE_SPEC_COMMON ]; then
  curl -sSL $URL_SPEC_COMMON -o $FILE_SPEC_COMMON
fi
if [ ! -f $FILE_SPEC_KEYS ]; then
  curl -sSL "https://converter.swagger.io/api/convert?url=$URL_SPEC_KEYS" \
    -H'Accept: application/yaml' \
    -o $FILE_SPEC_KEYS
  sed -i.orig -E 's/\{key-(name|version)\}/{key_\1}/g' $FILE_SPEC_KEYS
fi
if [ ! -f $FILE_SPEC_SECRETS ]; then
  curl -sSL "https://converter.swagger.io/api/convert?url=$URL_SPEC_SECRETS" \
    -H'Accept: application/yaml' \
    -o $FILE_SPEC_SECRETS
  sed -i.orig -E 's/\{secret-(name|version)\}/{secret_\1}/g' $FILE_SPEC_SECRETS
fi

cat <<EOF > $TARGET_DIR/openapi-merge.json
{
  "inputs": [
    {
      "inputFile": "keys.yaml",
      "dispute": {
        "prefix": "Keys"
      },
      "operationSelection": {
        "excludeTags": ["DeletedKeys"],
        "excludePaths": [
          {
            "path": "/deletedkeys/*",
            "method": "get"
          },
          {
            "path": "/deletedkeys/*",
            "method": "post"
          },
          {
            "path": "/deletedkeys/*",
            "method": "put"
          },
          {
            "path": "/deletedkeys/*",
            "method": "delete"
          }
        ]
      }
    },
    {
      "inputFile": "secrets.yaml",
      "dispute": {
        "prefix": "Secrets"
      },
      "operationSelection": {
        "excludeTags": ["DeletedSecrets"],
        "excludePaths": [
          {
            "path": "/deletedsecrets/*",
            "method": "get"
          },
          {
            "path": "/deletedsecrets/*",
            "method": "post"
          },
          {
            "path": "/deletedsecrets/*",
            "method": "put"
          },
          {
            "path": "/deletedsecrets/*",
            "method": "delete"
          }
        ]
      }
    }
  ],
  "output": "KeyVault.json",
}
EOF

# See https://www.npmjs.com/package/openapi-merge-cli
pnpm -C $BASE_PATH cli --config v$API_VERSION/openapi-merge.json
