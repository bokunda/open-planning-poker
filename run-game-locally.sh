#!/bin/bash

# Ask the user whether to get monitoring services
read -p "Get monitoring services as well? (Y/N): " monitoring

# Normalize input to lowercase for consistency
monitoring=$(echo "$monitoring" | tr '[:upper:]' '[:lower:]')

if [[ "$monitoring" == "y" ]]; then
    monitoring="yes"
else
    monitoring="no"
fi

# Get the parent directory of the current folder
parentDir=$(dirname "$(pwd)")
repoNames=(
    "open-planning-poker-graphql-gateway"
    "open-planning-poker-game-engine"
    "open-planning-poker-user-management"
    "open-planning-poker-web-app"
)

# Uncomment the following line if monitoring services should be included
# if [[ "$monitoring" == "yes" ]]; then
#     repoNames+=("open-planning-poker-monitoring-services")
# fi

# Remove existing folders if they exist
for repo in "${repoNames[@]}"; do
    repoPath="$parentDir/$repo"
    if [[ -d "$repoPath" ]]; then
        echo "Removing existing folder: $repoPath"
        rm -rf "$repoPath"
    fi
done

# Clone repositories
cd "$parentDir"  # Move to the parent directory
for repo in "${repoNames[@]}"; do
    repoUrl="https://github.com/bokunda/$repo.git"
    echo "Cloning: $repoUrl"
    git clone "$repoUrl"
done

# Move back to the initial directory
cd open-planning-poker

# Uncomment to stop and remove old containers if needed
# echo "Stopping and removing old containers..."
# docker-compose down

# Run docker-compose
echo "Running docker-compose up"
docker-compose up
