# Pack UserManagement schema
cd ../../UserManagement/OpenPlanningPoker.UserManagement.GraphQL
dotnet run -- schema export --output schema.graphql
fusion subgraph pack

# Pack GameEngine schema
cd ../../GameEngine/OpenPlanningPoker.GameEngine.GraphQL
dotnet run -- schema export --output schema.graphql
fusion subgraph pack

# Go To Gateway
cd ../../Gateway/OpenPlanningPoker.Fusion.Gateway
# Compose schema

fusion compose -p gateway.fgp -s ../../UserManagement/OpenPlanningPoker.UserManagement.GraphQL
fusion compose -p gateway.fgp -s ../../GameEngine/OpenPlanningPoker.GameEngine.GraphQL