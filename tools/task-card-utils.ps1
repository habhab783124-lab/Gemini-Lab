function Get-TaskCardData {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TaskCardPath
    )

    if (-not (Test-Path -LiteralPath $TaskCardPath)) {
        throw "Task card not found: $TaskCardPath"
    }

    try {
        return (Get-Content -LiteralPath $TaskCardPath -Raw -Encoding UTF8 | ConvertFrom-Json)
    }
    catch {
        throw "Task card is not valid JSON: $TaskCardPath. $($_.Exception.Message)"
    }
}

function ConvertTo-TaskRepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PathValue
    )

    return (($PathValue -replace '\\', '/') -replace '^\./', '').Trim()
}

function Get-TaskPlanPayload {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Task
    )

    # Mutable execution fields (status, approval time, verification notes and
    # the hash itself) are intentionally excluded from the plan fingerprint.
    return [ordered]@{
        workflow_contract_version   = [int]$Task.workflow_contract_version
        task_id                     = [string]$Task.task_id
        task_source                 = [string]$Task.task_source
        source_excerpt              = [string]$Task.source_excerpt
        approval_scope              = [string]$Task.approval_scope
        scope_do                    = @($Task.scope_do)
        scope_not_do                = @($Task.scope_not_do)
        completion_criteria         = @($Task.completion_criteria)
        direct_files                = @($Task.direct_files | ForEach-Object { ConvertTo-TaskRepoPath ([string]$_) })
        scene_play_parity_required  = [bool]$Task.scene_play_parity_required
        scene_visual_contracts      = @($Task.scene_visual_contracts)
        runtime_visual_files        = @($Task.runtime_visual_files | ForEach-Object { ConvertTo-TaskRepoPath ([string]$_) })
        verification_commands       = @($Task.verification_commands)
        risks                       = @($Task.risks)
        baseline_strategy           = [string]$Task.baseline_strategy
    }
}

function Get-TaskPlanHash {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Task
    )

    $payloadJson = Get-TaskPlanPayload $Task | ConvertTo-Json -Depth 50 -Compress
    $bytes = [Text.Encoding]::UTF8.GetBytes($payloadJson)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $digest = $sha.ComputeHash($bytes)
    }
    finally {
        $sha.Dispose()
    }

    return "sha256:$(-join ($digest | ForEach-Object { $_.ToString('x2') }))"
}
