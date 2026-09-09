param(
    [ValidateSet("CreateBaseline", "Validate")]
    [string]$Mode = "Validate",
    [string]$TaskCardPath = "",
    [string]$BaselinePath = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($TaskCardPath)) {
    $TaskCardPath = Join-Path $repoRoot "docs/current-task-card.json"
}

. (Join-Path $PSScriptRoot "task-card-utils.ps1")
$task = Get-TaskCardData $TaskCardPath
$taskId = [string]$task.task_id
if ([string]::IsNullOrWhiteSpace($taskId)) {
    throw "Task scope check requires task_id."
}

if ([string]::IsNullOrWhiteSpace($BaselinePath)) {
    $safeTaskId = $taskId -replace '[^A-Za-z0-9_.-]', '_'
    $BaselinePath = Join-Path ([IO.Path]::GetTempPath()) "GeminiLab-task-$safeTaskId-baseline.json"
}

function Get-RepoStatusPaths {
    $lines = @(git -c core.quotePath=false -C $repoRoot status --porcelain=v1 --untracked-files=all)
    $paths = New-Object System.Collections.Generic.List[string]
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.Length -lt 4) { continue }
        $pathText = $line.Substring(3).Trim()
        if ($pathText.Length -ge 2 -and $pathText.StartsWith('"') -and $pathText.EndsWith('"')) {
            $pathText = $pathText.Substring(1, $pathText.Length - 2)
        }
        if ($pathText -match ' -> ') {
            $pathText = $pathText.Split(' -> ')[-1]
        }
        $paths.Add((ConvertTo-TaskRepoPath $pathText))
    }
    return @($paths | Sort-Object -Unique)
}

function Get-FileDigest([string]$repoPath) {
    $fullPath = Join-Path $repoRoot ($repoPath -replace '/', '\')
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        return $null
    }
    return (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
}

$allowed = @($task.direct_files | ForEach-Object { ConvertTo-TaskRepoPath ([string]$_) })

if ($Mode -eq "CreateBaseline") {
    $statusPaths = Get-RepoStatusPaths
    $hashes = [ordered]@{}
    foreach ($statusPath in $statusPaths) {
        $hashes[$statusPath] = Get-FileDigest $statusPath
    }

    $baseline = [ordered]@{
        task_id = $taskId
        created_at_utc = [DateTime]::UtcNow.ToString("o")
        status_paths = $statusPaths
        path_hashes = $hashes
    }
    $parent = Split-Path -Parent $BaselinePath
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    $baseline | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $BaselinePath -Encoding UTF8
    [ordered]@{
        passed = $true
        mode = $Mode
        task_id = $taskId
        baseline_path = $BaselinePath
        baseline_paths = $statusPaths.Count
    } | ConvertTo-Json -Compress
    exit 0
}

if (-not (Test-Path -LiteralPath $BaselinePath)) {
    [ordered]@{
        passed = $false
        mode = $Mode
        task_id = $taskId
        error = "Missing task baseline. Run check-task-scope.ps1 -Mode CreateBaseline after the task gate passes."
        baseline_path = $BaselinePath
    } | ConvertTo-Json -Compress
    exit 1
}

$baseline = Get-Content -LiteralPath $BaselinePath -Raw -Encoding UTF8 | ConvertFrom-Json
if ([string]$baseline.task_id -ne $taskId) {
    throw "Task baseline belongs to '$($baseline.task_id)', not '$taskId'."
}

$baselinePaths = @($baseline.status_paths | ForEach-Object { ConvertTo-TaskRepoPath ([string]$_) })
$currentPaths = Get-RepoStatusPaths
$unexpectedNew = @($currentPaths | Where-Object { $baselinePaths -notcontains $_ -and $allowed -notcontains $_ })
$baselineHashMap = @{}
if ($null -ne $baseline.path_hashes) {
    foreach ($property in $baseline.path_hashes.PSObject.Properties) {
        $baselineHashMap[(ConvertTo-TaskRepoPath $property.Name)] = [string]$property.Value
    }
}

$unexpectedExistingChanges = New-Object System.Collections.Generic.List[string]
foreach ($baselineRepoPath in $baselinePaths) {
    if ($allowed -contains $baselineRepoPath) { continue }
    $expected = $baselineHashMap[$baselineRepoPath]
    $actual = Get-FileDigest $baselineRepoPath
    if ($expected -ne $actual) {
        $unexpectedExistingChanges.Add($baselineRepoPath)
    }
}

$failures = @($unexpectedNew + @($unexpectedExistingChanges)) | Sort-Object -Unique
$result = [ordered]@{
    passed = ($failures.Count -eq 0)
    mode = $Mode
    task_id = $taskId
    baseline_path = $BaselinePath
    allowed_file_count = $allowed.Count
    current_status_path_count = $currentPaths.Count
    unexpected_paths = $failures
}
$result | ConvertTo-Json -Depth 20 -Compress
if ($failures.Count -gt 0) { exit 1 }
exit 0
