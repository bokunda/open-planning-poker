## Fusion compose (Fusion v2, Hot Chocolate 16)

The gateway serves `gateway.far`, composed from both subgraphs with the Nitro CLI:

- Install the CLI: `dotnet tool install -g ChilliCream.Nitro.CommandLine --version 16.7.7`
- Compose: `./compose-gateway.sh` (or `./generate-schema.ps1` on Windows)

Each subgraph's name and URL live in its `schema-settings.json` (Docker DNS addresses). A field or type that both
subgraphs define must be `@shareable` (`[Shareable]`, or `ShareableTypesInterceptor` for generated types).

The scripts compose into the committed `gateway.far`, which also stores the composition settings. It sets
`defaultListSize` to 1, so the gateway's cost analysis counts a list without `@listSize` as one item, as the
subgraphs do. Without it such lists count as infinite and the gateway rejects every operation that selects one
(HC0047). If the archive is ever recreated from scratch, set `merger.defaultListSize` in its
`composition-settings.json` again; the composed schema then carries `@fusion__cost_options(defaultListSize: 1)`.
