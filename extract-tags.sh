#!/bin/bash

REF_NAME="$1"
if [ -z "$REF_NAME" ]; then
    echo 'The <ref_name> paramter is required.'
    exit 1
fi

SEMVER_REGEX='^v?([0-9]+)\.([0-9]+)\.([0-9]+)(-.+)?$'
if ! [[ "$REF_NAME" =~ $SEMVER_REGEX ]]; then
    echo "Unsupported semver version: $REF_NAME"
  exit 1
fi

SEMVER_COMPONENTS_STR=$(echo "$REF_NAME" | sed -E "s/$SEMVER_REGEX/\1 \2 \3 \4/")
read -a SEMVER_COMPONENTS <<< "$SEMVER_COMPONENTS_STR"

echo "VER_MAJOR=${SEMVER_COMPONENTS[0]}"
echo "VER_MAJOR_MINOR=${SEMVER_COMPONENTS[0]}.${SEMVER_COMPONENTS[1]}"
echo "VER_MAJOR_MINOR_PATCH=${SEMVER_COMPONENTS[0]}.${SEMVER_COMPONENTS[1]}.${SEMVER_COMPONENTS[2]}"
echo "VER_FULL=${SEMVER_COMPONENTS[0]}.${SEMVER_COMPONENTS[1]}.${SEMVER_COMPONENTS[2]}${SEMVER_COMPONENTS[3]}"
