#!/bin/sh
# Composes gateway.far, the Fusion v2 archive the gateway serves, from the two subgraphs.
# Needs the .NET 10 SDK and the Nitro CLI: dotnet tool install -g ChilliCream.Nitro.CommandLine --version 16.7.7
# Subgraph names and URLs come from each subgraph's committed schema-settings.json (Docker DNS addresses).
set -e
backend="$(cd "$(dirname "$0")/.." && pwd)"
gateway="$backend/open-planning-poker-graphql-gateway"

for subgraph in \
  open-planning-poker-user-management/src/OpenPlanningPoker.UserManagement.GraphQL \
  open-planning-poker-game-engine/src/OpenPlanningPoker.GameEngine.GraphQL
do
  dir="$backend/$subgraph"
  out="$dir/obj/fusion"
  rm -rf "$out" && mkdir -p "$out"
  (cd "$dir" && dotnet run -- schema export --output "$out/schema.graphqls")
  # The export writes the default schema name ("_Default"); the committed settings name the subgraph.
  cp "$dir/schema-settings.json" "$out/schema-settings.json"
done

# Compose into the committed archive: it keeps the composition settings stored in it, notably
# defaultListSize 1 (lists without @listSize count as one item, as in the subgraphs). Without it the
# gateway's cost analysis treats such lists as infinite and rejects every operation that selects one.
nitro fusion compose \
  -f "$backend/open-planning-poker-user-management/src/OpenPlanningPoker.UserManagement.GraphQL/obj/fusion" \
  -f "$backend/open-planning-poker-game-engine/src/OpenPlanningPoker.GameEngine.GraphQL/obj/fusion" \
  -a "$gateway/gateway.far"
echo "Composed $gateway/gateway.far"
