#!/bin/bash
set -e

current_directory="$PWD"

cd $(dirname $0)/..

pnpm install --frozen-lockfile
pnpm release

result=$?

cd "$current_directory"

exit $result
