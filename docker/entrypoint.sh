#!/bin/bash
set -e

# Auto-generate a secret if the user hasn't provided one
if grep -q '"secret": "GENERATE"' /app/config.json; then
  SECRET=$(openssl rand -hex 32)
  sed "s/\"secret\": \"GENERATE\"/\"secret\": \"$SECRET\"/" /app/config.json > /tmp/config.json
  cp /tmp/config.json /app/config.json
  rm /tmp/config.json
  echo "Generated random server secret."
fi

echo "Running db-init..."
yes y | dotnet FSO.Server.Core.dll db-init || true

echo "Starting server..."
exec dotnet FSO.Server.Core.dll run
