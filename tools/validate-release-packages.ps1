[CmdletBinding()]
param(
    [string]$InventoryPath = "tools/release-packages.json",
    [string]$PackageDirectory,
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedInventoryPath = Join-Path $repoRoot $InventoryPath

function Resolve-RepoPath {
    param([Parameter(Mandatory)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return (Join-Path $repoRoot $Path)
}

function Get-XmlProperty {
    param(
        [Parameter(Mandatory)][xml]$Project,
        [Parameter(Mandatory)][string]$Name
    )

    foreach ($group in @($Project.Project.PropertyGroup)) {
        $node = $group.$Name
        if ($null -ne $node) {
            $value = ([string]$node).Trim()
            if ($value.Length -gt 0) {
                return $value
            }
        }
    }

    return $null
}

function Assert-EqualSet {
    param(
        [Parameter(Mandatory)][string[]]$Expected,
        [Parameter(Mandatory)][string[]]$Actual,
        [Parameter(Mandatory)][string]$Label
    )

    $missing = @($Expected | Where-Object { $_ -notin $Actual })
    $unexpected = @($Actual | Where-Object { $_ -notin $Expected })

    if ($missing.Count -gt 0 -or $unexpected.Count -gt 0) {
        throw "$Label mismatch. Missing: $($missing -join ', '); unexpected: $($unexpected -join ', ')"
    }
}

function Get-NuspecMetadata {
    param([Parameter(Mandatory)][System.IO.FileInfo]$Package)

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Package.FullName)
    try {
        $entry = $archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec', [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
        if ($null -eq $entry) {
            throw "Package '$($Package.Name)' does not contain a .nuspec file."
        }

        $reader = [System.IO.StreamReader]::new($entry.Open())
        try {
            [xml]$nuspec = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }

    $metadata = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
    if ($null -eq $metadata) {
        throw "Package '$($Package.Name)' has no nuspec metadata element."
    }

    $dependencyNodes = @($metadata.SelectNodes(".//*[local-name()='dependency']"))
    $dependencies = @($dependencyNodes | ForEach-Object {
        [pscustomobject]@{
            Id = $_.Attributes['id'].Value
            Version = $_.Attributes['version'].Value
        }
    })

    $repository = $metadata.SelectSingleNode("*[local-name()='repository']")

    return [pscustomobject]@{
        Id = $metadata.SelectSingleNode("*[local-name()='id']").InnerText
        Version = $metadata.SelectSingleNode("*[local-name()='version']").InnerText
        Description = $metadata.SelectSingleNode("*[local-name()='description']").InnerText
        Authors = $metadata.SelectSingleNode("*[local-name()='authors']").InnerText
        License = $metadata.SelectSingleNode("*[local-name()='license']").InnerText
        ProjectUrl = $metadata.SelectSingleNode("*[local-name()='projectUrl']").InnerText
        RepositoryUrl = if ($null -ne $repository -and $null -ne $repository.Attributes['url']) { $repository.Attributes['url'].Value } else { $null }
        Tags = $metadata.SelectSingleNode("*[local-name()='tags']").InnerText
        Readme = $metadata.SelectSingleNode("*[local-name()='readme']").InnerText
        Dependencies = $dependencies
    }
}

if (-not (Test-Path -LiteralPath $resolvedInventoryPath)) {
    throw "Release package inventory not found: $resolvedInventoryPath"
}

$inventory = Get-Content -LiteralPath $resolvedInventoryPath -Raw | ConvertFrom-Json
$expectedPackages = @($inventory.packages)
$expectedIds = @($expectedPackages | ForEach-Object { $_.packageId })

if ($expectedIds.Count -eq 0) {
    throw "Release package inventory contains no packages."
}

$rootSourceProjects = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot "src") -Recurse -Filter "*.csproj" |
    Where-Object { $_.FullName -notmatch '[\\/]submodules[\\/]' })

$discoveredPackableIds = @()
$requiredProperties = @(
    "PackageId",
    "Description",
    "Authors",
    "Company",
    "PackageLicenseExpression",
    "PackageProjectUrl",
    "RepositoryUrl",
    "PackageTags",
    "PackageReadmeFile"
)

foreach ($projectFile in $rootSourceProjects) {
    [xml]$project = Get-Content -LiteralPath $projectFile.FullName -Raw
    $isPackable = (Get-XmlProperty -Project $project -Name "IsPackable")
    $relativeProject = [System.IO.Path]::GetRelativePath($repoRoot, $projectFile.FullName).Replace('\', '/')

    if ($isPackable -eq "true") {
        $packageId = Get-XmlProperty -Project $project -Name "PackageId"
        if ([string]::IsNullOrWhiteSpace($packageId)) {
            throw "Packable project '$relativeProject' must declare PackageId."
        }

        $discoveredPackableIds += $packageId

        foreach ($propertyName in $requiredProperties) {
            $propertyValue = Get-XmlProperty -Project $project -Name $propertyName
            if ([string]::IsNullOrWhiteSpace($propertyValue)) {
                throw "Packable project '$relativeProject' is missing required NuGet metadata '$propertyName'."
            }
        }

        $readmePath = Join-Path $projectFile.DirectoryName (Get-XmlProperty -Project $project -Name "PackageReadmeFile")
        if (-not (Test-Path -LiteralPath $readmePath)) {
            throw "Packable project '$relativeProject' declares a PackageReadmeFile that does not exist: $readmePath"
        }

        $packedReadme = @($project.Project.ItemGroup | ForEach-Object { $_.None } | Where-Object {
            $_.Include -eq "README.md" -and $_.Pack -eq "true" -and $_.PackagePath -eq "\"
        })

        if ($packedReadme.Count -eq 0) {
            throw "Packable project '$relativeProject' must pack README.md at the package root."
        }
    }
}

Assert-EqualSet -Expected $expectedIds -Actual $discoveredPackableIds -Label "Packable package inventory"

foreach ($projectPath in @($inventory.nonPackableProjects)) {
    $resolvedProjectPath = Resolve-RepoPath -Path $projectPath
    if (-not (Test-Path -LiteralPath $resolvedProjectPath)) {
        throw "Expected non-packable project does not exist: $projectPath"
    }

    [xml]$project = Get-Content -LiteralPath $resolvedProjectPath -Raw
    $isPackable = Get-XmlProperty -Project $project -Name "IsPackable"
    if ($isPackable -ne "false") {
        throw "Project '$projectPath' must remain non-packable."
    }
}

foreach ($package in $expectedPackages) {
    $projectPath = Resolve-RepoPath -Path $package.project
    if (-not (Test-Path -LiteralPath $projectPath)) {
        throw "Approved package project does not exist: $($package.project)"
    }

    [xml]$project = Get-Content -LiteralPath $projectPath -Raw
    $packageId = Get-XmlProperty -Project $project -Name "PackageId"

    # Case-sensitive comparison: NuGet locks the first-pushed casing of a PackageId forever,
    # so a lowercase typo in csproj that differs from the canonical inventory casing must
    # fail validation rather than silently slip through PowerShell's default `-ne` (case-insensitive).
    if ($packageId -cne $package.packageId) {
        throw "Inventory package '$($package.packageId)' does not match project PackageId '$packageId' (case-sensitive)."
    }
}

if (-not [string]::IsNullOrWhiteSpace($PackageDirectory)) {
    $resolvedPackageDirectory = Resolve-RepoPath -Path $PackageDirectory
    if (-not (Test-Path -LiteralPath $resolvedPackageDirectory)) {
        throw "Package directory not found: $resolvedPackageDirectory"
    }

    $packages = @(Get-ChildItem -LiteralPath $resolvedPackageDirectory -Filter "*.nupkg" |
        Where-Object { $_.Name -notlike "*.snupkg" })

    if ($packages.Count -eq 0) {
        throw "No .nupkg files found in $resolvedPackageDirectory"
    }

    $metadata = @($packages | ForEach-Object { Get-NuspecMetadata -Package $_ })
    $actualPackageIds = @($metadata | ForEach-Object { $_.Id })
    Assert-EqualSet -Expected $expectedIds -Actual $actualPackageIds -Label "Generated NuGet package inventory"

    foreach ($item in $metadata) {
        if (-not [string]::IsNullOrWhiteSpace($Version) -and $item.Version -ne $Version) {
            throw "Package '$($item.Id)' has version '$($item.Version)', expected '$Version'."
        }

        foreach ($field in @("Description", "Authors", "License", "ProjectUrl", "RepositoryUrl", "Tags", "Readme")) {
            if ([string]::IsNullOrWhiteSpace($item.$field)) {
                throw "Package '$($item.Id)' has empty nuspec metadata '$field'."
            }
        }

        foreach ($dependency in @($item.Dependencies | Where-Object { $_.Id -in $expectedIds })) {
            if ([string]::IsNullOrWhiteSpace($Version)) {
                continue
            }

            # Internal cross-package dependency versions must equal the release version exactly.
            # NuGet emits a dependency like '[1.0.0, )' (lower-bound inclusive range) by default;
            # parse that explicitly so '1.0.0' is not treated as matching '1.0.10' by substring.
            $dependencyVersion = $dependency.Version
            $rangeMatch = [regex]::Match($dependencyVersion, '^\s*\[\s*([^,\s\]]+)')
            $exactVersion = if ($rangeMatch.Success) { $rangeMatch.Groups[1].Value } else { $dependencyVersion.Trim() }

            if ($exactVersion -ne $Version) {
                throw "Package '$($item.Id)' references internal dependency '$($dependency.Id)' with version '$dependencyVersion' (resolved exact='$exactVersion'), expected '$Version'."
            }
        }
    }
}

Write-Host "Release package validation passed."
