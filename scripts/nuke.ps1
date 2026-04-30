$label = "com.docker.compose.project=skoleplanen"

$containers = docker ps -aq --filter "label=$label"
if ($containers) {
    Write-Host "Removing containers..."
    docker rm -f $containers
} else {
    Write-Host "No containers found."
}

$volumes = docker volume ls -q | Where-Object { $_ -like "skoleplanen-*" }
if ($volumes) {
    Write-Host "Removing volumes..."
    docker volume rm $volumes
} else {
    Write-Host "No volumes found."
}
