#!/bin/bash

BASE_DIR=$(dirname $0)
CERTS_DIR="$BASE_DIR/.certs"
EMULATOR_FQDN="${1:-emulator.vault.azure.net}"

mkdir -p $CERTS_DIR

openssl req -x509 -newkey rsa:2048 -sha256 -days 3560 -noenc \
  -keyout $CERTS_DIR/emulator.key \
  -out $CERTS_DIR/emulator.crt \
  -subj "/CN=$EMULATOR_FQDN" \
  -addext "subjectAltName=DNS:localhost,DNS:localhost.vault.azure.net,DNS:$EMULATOR_FQDN,IP:127.0.0.1"

openssl pkcs12 -export \
  -out $CERTS_DIR/emulator.pfx \
  -inkey $CERTS_DIR/emulator.key \
  -in $CERTS_DIR/emulator.crt
