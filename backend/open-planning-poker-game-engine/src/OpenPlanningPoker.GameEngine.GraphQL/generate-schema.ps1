# Pack UserManagement schema
Set-Location ../../open-planning-poker-user-management/src/OpenPlanningPoker.UserManagement.GraphQL
dotnet run -- schema export --output schema.graphql
fusion subgraph pack

# Pack GameEngine schema
Set-Location ../../open-planning-poker-game-engine/src/OpenPlanningPoker.GameEngine.GraphQL
dotnet run -- schema export --output schema.graphql
fusion subgraph pack

# Go To Gateway
Set-Location ../../open-planning-poker-graphql-gateway
# Compose schema

fusion compose -p gateway.fgp -s ../open-planning-poker-user-management/src/OpenPlanningPoker.UserManagement.GraphQL
fusion compose -p gateway.fgp -s ../open-planning-poker-game-engine/src/OpenPlanningPoker.GameEngine.GraphQL