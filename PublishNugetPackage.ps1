Set-Location $PSScriptRoot

function Publish-Nuget
{
    [CmdletBinding()]
    param (
        [string]$ProjectName,
        [string]$Token,
        [string]$SolutionPath
    )
    process
    {
        $ProjectPath = Join-Path $SolutionPath -ChildPath "$ProjectName\$ProjectName.csproj"

        dotnet build $ProjectPath -c Release #/p:PackageVersion=$packageVersion

        $fName = Join-Path $SolutionPath -ChildPath "$ProjectName\bin\Release\$ProjectName.$PackageVersion.nupkg"

        dotnet nuget push $fName -k $Token --source "github"  --timeout 3000 # --skip-duplicate
    }
}

[xml]$projXML = Get-content -Path ./MPEGTS/MPEGTS.csproj

$packageVersion = $projXML.Project.PropertyGroup.PackageVersion

Write-Host "Publishing MPEGTS package version $packageVersion"


Write-Host "Enter Android acces token: " -NoNewline

$token = Read-host

Publish-Nuget -ProjectName "MPEGTS" -SolutionPath $PSScriptRoot -Token $token
