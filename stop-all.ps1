$ErrorActionPreference = "Stop"

$ports = @(5143, 5082, 5291, 5175)
$stoppedAny = $false

foreach ($port in $ports) {
    $connections = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue

    foreach ($connection in $connections) {
        $processId = $connection.OwningProcess
        if ($processId -and $processId -ne 0) {
            try {
                $process = Get-Process -Id $processId -ErrorAction Stop
                Stop-Process -Id $processId -Force -ErrorAction Stop
                Write-Host "[Stopped] Port $port -> $($process.ProcessName) ($processId)" -ForegroundColor Yellow
                $stoppedAny = $true
            }
            catch {
                Write-Host ("[Skipped] Could not stop process on port {0}: {1}" -f $port, $processId) -ForegroundColor DarkYellow
            }
        }
    }
}

if (-not $stoppedAny) {
    Write-Host "No listening services were found on 5143 / 5082 / 5291 / 5175." -ForegroundColor Cyan
}
