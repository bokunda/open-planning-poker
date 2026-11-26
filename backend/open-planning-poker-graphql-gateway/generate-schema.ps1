# Recomposes gateway.far (Fusion v2) from both subgraphs. Needs the .NET 10 SDK and the Nitro CLI:
#   dotnet tool install -g ChilliCream.Nitro.CommandLine --version 16.7.7
$ErrorActionPreference = "Stop"
$backend = Resolve-Path "$PSScriptRoot/.."
$subgraphs = @(
    "open-planning-poker-user-management/src/OpenPlanningPoker.UserManagement.GraphQL",
    "open-planning-poker-game-engine/src/OpenPlanningPoker.GameEngine.GraphQL"
)
$composeArgs = @()
foreach ($subgraph in $subgraphs) {
    $dir = Join-Path $backend $subgraph
    $out = Join-Path $dir "obj/fusion"
    if (Test-Path $out) { Remove-Item -Recurse -Force $out }
    New-Item -ItemType Directory -Force $out | Out-Null
    Push-Location $dir
    dotnet run -- schema export --output (Join-Path $out "schema.graphqls")
    if ($LASTEXITCODE -ne 0) { Pop-Location; throw "Schema export failed for $subgraph" }
    Pop-Location
    # The export writes the default schema name ("_Default"); the committed settings name the subgraph.
    Copy-Item (Join-Path $dir "schema-settings.json") (Join-Path $out "schema-settings.json") -Force
    $composeArgs += @("-f", $out)
}
$archive = Join-Path $PSScriptRoot "gateway.far"
if (Test-Path $archive) { Remove-Item -Force $archive }
nitro fusion compose @composeArgs -a $archive
if ($LASTEXITCODE -ne 0) { throw "Composition failed" }
