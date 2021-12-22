[CmdletBinding()]
param (
    [string[]]
    [ValidateSet("WebClient", "Worker")]
    $Projects = @("WebClient", "Worker"),
    [Switch]
    $SkipDocker,
    [Switch]
    $skipPublish
)

function Publish-DotNetProject($projectName, $publishLocation, $configuration){
    Write-Host "Publishing $projectName to $publishLocation"
    $csproj = "$projectName/$projectName.csproj"
    if(Test-Path $publishLocation){
        Remove-Item -Recurse -Force $publishLocation
    }
    dotnet publish -c $configuration -o $publishLocation $csproj
}

function Build-DockerImage($project, $publishLocation){
    Write-Host "Building docker image "
    $tagName = "bms-$($project.ToLower())";
    docker build -t $tagName -f ./$project/DockerFile --build-arg PUBLISH_DIR=$publishLocation .
}

Write-Host "Publishing $([string]::Join(", ", $Projects))"
Write-Host "Skip publish: $SkipDocker"
Write-Host "Skip docker: $SkipDocker"

foreach($project in $Projects){
    Write-Host "Building $project"

    $configuration = "Release"
    $publishLocation = "$project/bin/$configuration/dockerpublish"

    if(!$skipPublish){
        Publish-DotNetProject $project $publishLocation $configuration
    }else{
        Write-Host "Skipped publishing $project"
    }

    if(!$skipDocker){
        Build-DockerImage $project $publishLocation
    }else{
        Write-Host "Skipped docker build for $project"
    }
}
