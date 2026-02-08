#!/bin/bash
set -e

echo "Running db-init..."
yes y | dotnet FSO.Server.Core.dll db-init || true

echo "Starting server..."
exec dotnet FSO.Server.Core.dll run
