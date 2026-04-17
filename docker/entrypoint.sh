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

# Wait for MariaDB: podman-compose does not block on healthcheck reliably
echo "Waiting for database to accept connections..."
DB_HOST="${DB_HOST:-mariadb}"
DB_PORT="${DB_PORT:-3306}"
for i in $(seq 1 60); do
  if (echo > /dev/tcp/${DB_HOST}/${DB_PORT}) 2>/dev/null; then
    echo "Database reachable after ${i}s"
    break
  fi
  sleep 1
done

echo "Running db-init..."
yes y | dotnet FSO.Server.Core.dll db-init || true

echo "Starting server..."
exec dotnet FSO.Server.Core.dll run
