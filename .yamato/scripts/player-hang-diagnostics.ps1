# Diagnostics for the Win ARM64 standalone player that stalls ~30s at shutdown (ABC-556).
# Started in the background before the tests. Waits for PlayerWithTests.exe, then samples it
# at fixed offsets and writes everything to -OutDir so it is uploaded as a job artifact.
param(
    [string]$OutDir = "hang-diagnostics",
    [string]$ProcessName = "PlayerWithTests",
    [int[]]$SampleSeconds = @(12, 22, 32),
    [int]$DumpAtSample = 1,
    [int]$WaitForPlayerSeconds = 900
)

$ErrorActionPreference = "Continue"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Start-Transcript -Path (Join-Path $OutDir "watcher.log") | Out-Null

function Save-Screenshot($path) {
    try {
        Add-Type -AssemblyName System.Windows.Forms, System.Drawing
        $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
        $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
        [System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    } catch { Write-Host "Screenshot failed: $_" }
}

Write-Host "Waiting for $ProcessName.exe"
$deadline = (Get-Date).AddSeconds($WaitForPlayerSeconds)
$player = $null
while ((Get-Date) -lt $deadline -and -not $player) {
    $player = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $player) { Start-Sleep -Milliseconds 250 }
}
if (-not $player) { Write-Host "Player never started"; Stop-Transcript | Out-Null; exit 0 }

$started = Get-Date
$procId = $player.Id
Write-Host "Player pid $procId seen at $($started.ToString('HH:mm:ss.fff'))"

for ($i = 0; $i -lt $SampleSeconds.Count; $i++) {
    $at = $started.AddSeconds($SampleSeconds[$i])
    while ((Get-Date) -lt $at) { Start-Sleep -Milliseconds 250 }
    $tag = "t{0:D2}" -f $SampleSeconds[$i]
    $p = Get-Process -Id $procId -ErrorAction SilentlyContinue
    if (-not $p) { Write-Host "[$tag] player already exited"; break }
    Write-Host "[$tag] player alive at $((Get-Date).ToString('HH:mm:ss.fff'))"

    $p.Threads | Select-Object Id, ThreadState, WaitReason, StartTime, TotalProcessorTime |
        Format-Table -AutoSize | Out-String -Width 300 | Set-Content (Join-Path $OutDir "$tag-threads.txt")
    netstat -ano | Out-File (Join-Path $OutDir "$tag-netstat.txt")
    Get-CimInstance Win32_Process | Select-Object Caption, ProcessId, ParentProcessId, CommandLine |
        Format-Table -AutoSize | Out-String -Width 400 | Set-Content (Join-Path $OutDir "$tag-processes.txt")
    Get-NetFirewallRule -Enabled True -Direction Inbound -ErrorAction SilentlyContinue |
        Where-Object { $_.DisplayName -match "Unity|Alembic|Player" } |
        Format-List DisplayName, Action, Profile, Enabled | Out-File (Join-Path $OutDir "$tag-firewall.txt")
    Save-Screenshot (Join-Path $OutDir "$tag-screenshot.png")

    if ($i -eq $DumpAtSample) {
        $dump = Join-Path (Resolve-Path $OutDir) "$tag-player.dmp"
        Write-Host "[$tag] writing minidump to $dump"
        rundll32.exe C:\Windows\System32\comsvcs.dll, MiniDump $procId $dump full
        Start-Sleep -Seconds 5
        if (Test-Path $dump) { Write-Host "Dump size: $((Get-Item $dump).Length) bytes" } else { Write-Host "Dump was not written" }
    }
}

Write-Host "Done at $((Get-Date).ToString('HH:mm:ss.fff'))"
Stop-Transcript | Out-Null
