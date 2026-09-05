param(
    [Parameter(ParameterSetName = 'Install', Mandatory = $true)]
    [switch] $Install,
    [Parameter(ParameterSetName = 'Install', Mandatory = $true)]
    [string] $Executable,
    [Parameter(ParameterSetName = 'Remove', Mandatory = $true)]
    [switch] $Remove
)

$ErrorActionPreference = 'Stop'
$taskName = 'LockKeyFlyout'

if ($Remove) {
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
    exit 0
}

$resolvedExecutable = [System.IO.Path]::GetFullPath($Executable)
if (-not [System.IO.File]::Exists($resolvedExecutable)) {
    throw "Executable not found: $resolvedExecutable"
}

$identity = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$action = New-ScheduledTaskAction `
    -Execute $resolvedExecutable `
    -WorkingDirectory ([System.IO.Path]::GetDirectoryName($resolvedExecutable))
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $identity
$principal = New-ScheduledTaskPrincipal `
    -UserId $identity `
    -LogonType Interactive `
    -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit ([TimeSpan]::Zero) `
    -MultipleInstances IgnoreNew

Register-ScheduledTask `
    -TaskName $taskName `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Settings $settings `
    -Description 'Starts LockKeyFlyout elevated when the current user signs in.' `
    -Force | Out-Null
