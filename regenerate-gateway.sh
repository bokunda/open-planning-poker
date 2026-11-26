#!/bin/sh
# Recomposes backend/open-planning-poker-graphql-gateway/gateway.far (Fusion v2) inside a .NET 10 SDK
# container with backend/ mounted at /src.
dotnet tool install -g ChilliCream.Nitro.CommandLine --version 16.7.7 2>/dev/null || true
export PATH="$PATH:/root/.dotnet/tools"
sh /src/open-planning-poker-graphql-gateway/compose-gateway.sh
echo "DONE - gateway.far regenerated"
