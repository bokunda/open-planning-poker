## Fusion compose (Fusion v2, Hot Chocolate 16)

The gateway serves `gateway.far`, composed from both subgraphs with the Nitro CLI:

- Install the CLI: `dotnet tool install -g ChilliCream.Nitro.CommandLine --version 16.7.7`
- Compose: `./compose-gateway.sh` (or `./generate-schema.ps1` on Windows)

Each subgraph's name and URL live in its `schema-settings.json` (Docker DNS addresses). A field or type that both
subgraphs define must be `@shareable` (`[Shareable]`, or `ShareableTypesInterceptor` for generated types).
