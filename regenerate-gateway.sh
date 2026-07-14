#!/bin/sh
dotnet tool install -g HotChocolate.Fusion.CommandLine 2>/dev/null || true
export PATH="$PATH:/root/.dotnet/tools"
cd /src/open-planning-poker-graphql-gateway
fusion compose -p gateway.fgp -s /src/open-planning-poker-user-management/src/OpenPlanningPoker.UserManagement.GraphQL
fusion compose -p gateway.fgp -s /src/open-planning-poker-game-engine/src/OpenPlanningPoker.GameEngine.GraphQL
echo "DONE - gateway.fgp regenerated"

echo "DONE - gateway.fgp regenerated"
