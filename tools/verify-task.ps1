param(
    [string]$TaskCardPath = "",
    [string]$BaselinePath = "",
    [switch]$JsonOnly
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($TaskCardPath)) {
    $TaskCardPath = Join-Path $repoRoot "docs/current-task-card.json"
}

$task = Get-Content -LiteralPath $TaskCardPath -Raw -Encoding UTF8 | ConvertFrom-Json
$results = New-Object System.Collections.Generic.List[object]

function Add-CheckResult {
    param(
        [string]$Name,
        [bool]$Passed,
        [int]$ExitCode,
        [string]$Output = ""
    )
    [void]$results.Add([ordered]@{
        name = $Name
        passed = $Passed
        exit_code = $ExitCode
        output = $Output.Trim()
    })
}

function Invoke-PowerShellCheck {
    param(
        [string]$Name,
        [string]$ScriptPath,
        [string[]]$Arguments
    )
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $ScriptPath @Arguments 2>&1 | Out-String
    $code = $LASTEXITCODE
    Add-CheckResult -Name $Name -Passed ($code -eq 0) -ExitCode $code -Output $output
}

Invoke-PowerShellCheck -Name "task-gate-review" -ScriptPath (Join-Path $repoRoot "tools/check-task-gate.ps1") -Arguments @("-Mode", "review", "-TaskCardPath", $TaskCardPath)

$scopeArguments = @("-Mode", "Validate", "-TaskCardPath", $TaskCardPath)
if (-not [string]::IsNullOrWhiteSpace($BaselinePath)) {
    $scopeArguments += @("-BaselinePath", $BaselinePath)
}
Invoke-PowerShellCheck -Name "task-scope" -ScriptPath (Join-Path $repoRoot "tools/check-task-scope.ps1") -Arguments $scopeArguments

$previousErrorAction = $ErrorActionPreference
$ErrorActionPreference = "Continue"
$allowedDiffPaths = @($task.direct_files | ForEach-Object { ([string]$_) -replace '/', '\' })
$diffOutput = git -C $repoRoot diff --check -- $allowedDiffPaths 2>$null | Out-String
$ErrorActionPreference = $previousErrorAction
$diffCode = $LASTEXITCODE
Add-CheckResult -Name "git-diff-check" -Passed ($diffCode -eq 0) -ExitCode $diffCode -Output $diffOutput

$parseFailures = New-Object System.Collections.Generic.List[string]
foreach ($file in @($task.direct_files)) {
    $repoPath = ([string]$file) -replace '/', '\'
    if (-not $repoPath.EndsWith('.ps1', [StringComparison]::OrdinalIgnoreCase)) { continue }
    $fullPath = Join-Path $repoRoot $repoPath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { continue }
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($fullPath, [ref]$tokens, [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $parseFailures.Add(([string]$file))
    }
}
$parseOutput = if ($parseFailures.Count -eq 0) { "All task PowerShell files parsed successfully." } else { "Parse failures: $($parseFailures -join ', ')" }
Add-CheckResult -Name "powershell-parse" -Passed ($parseFailures.Count -eq 0) -ExitCode ($(if ($parseFailures.Count -eq 0) { 0 } else { 1 })) -Output $parseOutput

$passed = @($results | Where-Object { -not $_.passed }).Count -eq 0
$report = [ordered]@{
    task_id = [string]$task.task_id
    passed = $passed
    checks = $results.ToArray()
}
$json = $report | ConvertTo-Json -Depth 30
if ($JsonOnly) {
    Write-Output $json
}
else {
    Write-Output $json
}
if (-not $passed) { exit 1 }
exit 0
