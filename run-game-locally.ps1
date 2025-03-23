# Ask the user whether to get monitoring services
#$monitoring = Read-Host "Get monitoring services as well? (Y/N)"

# Normalize input to lowercase for consistency
if ($monitoring -eq "Y" -or $monitoring -eq "y") {
    $monitoring = "yes"
} else {
    $monitoring = "no"
}

# Get the parent directory of the current folder
$parentDir = (Get-Item "..").FullName
$repoNames = @(
    "open-planning-poker-graphql-gateway",
    "open-planning-poker-game-engine",
    "open-planning-poker-user-management",
    "open-planning-poker-web-app"
)

#if ($monitoring -eq "yes") {
    #$repoNames += "open-planning-poker-monitoring-services"
#}

# Remove existing folders if they exist
foreach ($repo in $repoNames) {
    $repoPath = Join-Path -Path $parentDir -ChildPath $repo
    if (Test-Path $repoPath) {
        Write-Host "Removing existing folder: $repoPath"
        Remove-Item -Recurse -Force $repoPath
    }
}

# Clone repositories
cd ..  # Move to the parent directory
foreach ($repo in $repoNames) {
    $repoUrl = "https://github.com/bokunda/$repo.git"
    Write-Host "Cloning: $repoUrl"
    git clone $repoUrl
}

# Move back to the initial directory
cd open-planning-poker

# Write-Host "Stopping and removing old containers..."
# docker-compose down

# Run docker-compose
Write-Host "Running docker-compose up"
docker-compose up

# Wait for user input before closing
Write-Host "Press any key to close..."
[System.Console]::ReadKey()
