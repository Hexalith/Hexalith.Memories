[CmdletBinding()]
param(
    [string]$InventoryPath = "tools/release-packages.json",
    [string]$SchemaPath = "tools/release-packages.schema.json",
    [string]$PackageDirectory,
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

function Resolve-RepoPath {
    param([Parameter(Mandatory)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return (Join-Path $repoRoot $Path)
}

$resolvedInventoryPath = Resolve-RepoPath -Path $InventoryPath
$resolvedSchemaPath = Resolve-RepoPath -Path $SchemaPath

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

function Get-RawXmlProperty {
    param(
        [Parameter(Mandatory)][xml]$Project,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$RelativeProject
    )

    # Returns the single matching property as-is (including blank), or $null if no element exists.
    # Multiple or conditional declarations are rejected so release inventory validation does not
    # silently diverge from the effective MSBuild property used by pack/build.
    $matches = @()
    foreach ($group in @($Project.Project.PropertyGroup)) {
        $nodes = @($group.ChildNodes | Where-Object {
            $_.NodeType -eq [System.Xml.XmlNodeType]::Element -and $_.Name -eq $Name
        })
        foreach ($node in $nodes) {
            $matches += [pscustomobject]@{
                Node = $node
                PropertyGroup = $group
            }
        }
    }

    if ($matches.Count -eq 0) {
        return $null
    }

    if ($matches.Count -gt 1) {
        throw "Project '$RelativeProject' declares <IsPackable> multiple times; use one unconditional value so release inventory enforcement is unambiguous."
    }

    $match = $matches[0]
    if ($null -ne $match.Node.Condition -or $null -ne $match.PropertyGroup.Condition) {
        throw "Project '$RelativeProject' declares conditional <IsPackable>; use one unconditional value so release inventory enforcement is unambiguous."
    }

    return $match.Node.InnerText
}

function Assert-EqualSet {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$Expected,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$Actual,
        [Parameter(Mandatory)][string]$Label
    )

    $missing = @($Expected | Where-Object { $_ -notin $Actual })
    $unexpected = @($Actual | Where-Object { $_ -notin $Expected })

    if ($missing.Count -gt 0 -or $unexpected.Count -gt 0) {
        throw "$Label mismatch. Missing: $($missing -join ', '); unexpected: $($unexpected -join ', ')"
    }
}

function Test-InventoryAgainstSchema {
    param(
        [Parameter(Mandatory)][string]$InventoryFile,
        [Parameter(Mandatory)][string]$SchemaFile
    )

    if (-not (Test-Path -LiteralPath $SchemaFile)) {
        throw "Release package inventory schema not found: $SchemaFile"
    }

    if ($null -eq (Get-Command Test-Json -ErrorAction SilentlyContinue)) {
        throw "The Test-Json cmdlet is required to validate '$InventoryFile' against '$SchemaFile' (PowerShell 7+ required). Install pwsh and rerun."
    }

    $rawJson = Get-Content -LiteralPath $InventoryFile -Raw

    $schemaErrors = $null
    $isValid = $false
    try {
        # ErrorAction Continue + ErrorVariable lets Test-Json report per-property mismatches
        # (misspelled fields like "packageID", "projectPath", "nonPackableProject") instead of
        # bubbling up only the first NJsonSchema exception.
        $isValid = Test-Json -Json $rawJson -SchemaFile $SchemaFile -ErrorAction SilentlyContinue -ErrorVariable schemaErrors
    }
    catch {
        throw "Schema validation of '$InventoryFile' against '$SchemaFile' threw an exception: $($_.Exception.Message)"
    }

    if (-not $isValid) {
        $detail = if ($schemaErrors -and $schemaErrors.Count -gt 0) {
            ($schemaErrors | ForEach-Object { $_.ToString() }) -join '; '
        }
        else {
            "(no diagnostic emitted)"
        }
        throw "Release package inventory '$InventoryFile' does not match schema '$SchemaFile': $detail"
    }
}

function ConvertTo-NormalizedNuGetVersion {
    param([Parameter(Mandatory)][string]$VersionInput)

    if ($VersionInput -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$') {
        throw "Version '$VersionInput' is not a valid semantic version."
    }

    $plus = $VersionInput.IndexOf('+')
    if ($plus -lt 0) {
        return [pscustomobject]@{
            Original = $VersionInput
            Normalized = $VersionInput
            HadBuildMetadata = $false
        }
    }

    return [pscustomobject]@{
        Original = $VersionInput
        Normalized = $VersionInput.Substring(0, $plus)
        HadBuildMetadata = $true
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

# Schema validation runs before any structural use so misspelled fields
# ("packageID", "projectPath", "nonPackableProject") are rejected before any
# downstream pack/publish script consumes the inventory (W12).
Test-InventoryAgainstSchema -InventoryFile $resolvedInventoryPath -SchemaFile $resolvedSchemaPath

$inventory = Get-Content -LiteralPath $resolvedInventoryPath -Raw | ConvertFrom-Json
$expectedPackages = @($inventory.packages)
$expectedIds = @($expectedPackages | ForEach-Object { $_.packageId })

if ($expectedIds.Count -eq 0) {
    throw "Release package inventory contains no packages."
}

# Duplicate guards run before set comparison so the diagnostic names duplicates
# rather than silently collapsing them to a single inventory entry.
$duplicateIds = @($expectedIds | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })
if ($duplicateIds.Count -gt 0) {
    throw "Inventory packages contain duplicate packageId values: $($duplicateIds -join ', ')"
}

$expectedPackageProjects = @($expectedPackages | ForEach-Object { $_.project })
$duplicatePackageProjects = @($expectedPackageProjects | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })
if ($duplicatePackageProjects.Count -gt 0) {
    throw "Inventory packages contain duplicate project paths: $($duplicatePackageProjects -join ', ')"
}

$expectedNonPackable = @($inventory.nonPackableProjects)
$duplicateNonPackable = @($expectedNonPackable | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name })
if ($duplicateNonPackable.Count -gt 0) {
    throw "Inventory contains duplicate nonPackableProjects entries: $($duplicateNonPackable -join ', ')"
}

$crossBucket = @($expectedNonPackable | Where-Object { $_ -in $expectedPackageProjects })
if ($crossBucket.Count -gt 0) {
    throw "Inventory paths declared in both packages and nonPackableProjects: $($crossBucket -join ', ')"
}

$rootSourceProjects = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot "src") -Recurse -Filter "*.csproj")

$discoveredPackableIds = @()
$discoveredPackablePaths = @()
$discoveredNonPackablePaths = @()

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
    $relativeProject = [System.IO.Path]::GetRelativePath($repoRoot, $projectFile.FullName).Replace('\', '/')

    $rawIsPackable = Get-RawXmlProperty -Project $project -Name "IsPackable" -RelativeProject $relativeProject
    if ($null -eq $rawIsPackable) {
        # W2: a missing <IsPackable> historically defaulted to true via SDK behavior,
        # but treating it as implicit was the silent-bypass risk this story closes.
        # Require an explicit declaration so every src/**/*.csproj is auditable.
        throw "Project '$relativeProject' is missing <IsPackable>; declare exactly 'true' or 'false' so release inventory enforcement is unambiguous."
    }

    $isPackable = $rawIsPackable.Trim()
    if ($isPackable.Length -eq 0) {
        throw "Project '$relativeProject' has a blank <IsPackable> value; declare exactly 'true' or 'false'."
    }
    if ($isPackable -ne 'true' -and $isPackable -ne 'false') {
        throw "Project '$relativeProject' has unsupported <IsPackable> value '$isPackable'; expected exactly 'true' or 'false'."
    }

    if ($isPackable -eq 'true') {
        $packageId = Get-XmlProperty -Project $project -Name "PackageId"
        if ([string]::IsNullOrWhiteSpace($packageId)) {
            throw "Packable project '$relativeProject' must declare PackageId."
        }

        $discoveredPackableIds += $packageId
        $discoveredPackablePaths += $relativeProject

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
    else {
        $discoveredNonPackablePaths += $relativeProject
    }
}

# W2: every source project must appear in exactly one inventory bucket.
Assert-EqualSet -Expected $expectedIds -Actual $discoveredPackableIds -Label "Packable package inventory"
Assert-EqualSet -Expected $expectedPackageProjects -Actual $discoveredPackablePaths -Label "Packable project inventory"
Assert-EqualSet -Expected $expectedNonPackable -Actual $discoveredNonPackablePaths -Label "Non-packable project inventory"

foreach ($projectPath in $expectedNonPackable) {
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

$normalizedVersion = $null
if (-not [string]::IsNullOrWhiteSpace($Version)) {
    # W15: NuGet strips '+...' build metadata from package versions during pack, so a direct
    # operator running this script with -Version '1.2.3+local' would otherwise see a confusing
    # mismatch error against '1.2.3'. Normalize here, name both forms in the diagnostic, and
    # use the normalized form for downstream comparisons (semantic-release versions and NuGet
    # package versions stay in lockstep).
    $versionResult = ConvertTo-NormalizedNuGetVersion -VersionInput $Version
    if ($versionResult.HadBuildMetadata) {
        Write-Host "Note: -Version '$($versionResult.Original)' carries build metadata; comparing against NuGet-normalized '$($versionResult.Normalized)' (NuGet pack strips '+...')."
    }
    $normalizedVersion = $versionResult.Normalized
}

if (-not [string]::IsNullOrWhiteSpace($PackageDirectory)) {
    $resolvedPackageDirectory = Resolve-RepoPath -Path $PackageDirectory
    if (-not (Test-Path -LiteralPath $resolvedPackageDirectory)) {
        throw "Package directory not found: $resolvedPackageDirectory"
    }

    # W16 (validate-release-packages.ps1 half): use Extension equality so the filter is explicit
    # about what it accepts. Equivalent set vs. the prior `-notlike "*.snupkg"` defensive guard.
    $packages = @(Get-ChildItem -LiteralPath $resolvedPackageDirectory -Filter "*.nupkg" |
        Where-Object { $_.Extension -ieq '.nupkg' })

    if ($packages.Count -eq 0) {
        throw "No .nupkg files found in $resolvedPackageDirectory"
    }

    $metadata = @($packages | ForEach-Object { Get-NuspecMetadata -Package $_ })
    $actualPackageIds = @($metadata | ForEach-Object { $_.Id })
    Assert-EqualSet -Expected $expectedIds -Actual $actualPackageIds -Label "Generated NuGet package inventory"

    foreach ($item in $metadata) {
        if (-not [string]::IsNullOrWhiteSpace($normalizedVersion) -and $item.Version -ne $normalizedVersion) {
            throw "Package '$($item.Id)' has version '$($item.Version)', expected '$normalizedVersion'."
        }

        foreach ($field in @("Description", "Authors", "License", "ProjectUrl", "RepositoryUrl", "Tags", "Readme")) {
            if ([string]::IsNullOrWhiteSpace($item.$field)) {
                throw "Package '$($item.Id)' has empty nuspec metadata '$field'."
            }
        }

        foreach ($dependency in @($item.Dependencies | Where-Object { $_.Id -in $expectedIds })) {
            if ([string]::IsNullOrWhiteSpace($normalizedVersion)) {
                continue
            }

            # Internal cross-package dependency versions must equal the release version exactly.
            # NuGet emits a dependency like '[1.0.0, )' (lower-bound inclusive range) by default;
            # parse that explicitly so '1.0.0' is not treated as matching '1.0.10' by substring.
            $dependencyVersion = $dependency.Version
            $rangeMatch = [regex]::Match($dependencyVersion, '^\s*\[\s*([^,\s\]]+)')
            $exactVersion = if ($rangeMatch.Success) { $rangeMatch.Groups[1].Value } else { $dependencyVersion.Trim() }

            if ($exactVersion -ne $normalizedVersion) {
                throw "Package '$($item.Id)' references internal dependency '$($dependency.Id)' with version '$dependencyVersion' (resolved exact='$exactVersion'), expected '$normalizedVersion'."
            }
        }
    }
}

Write-Host "Release package validation passed."
