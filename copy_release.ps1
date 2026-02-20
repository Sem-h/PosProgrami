$src = "c:\Users\Administrator\Desktop\Projes\PosProjesi\PosProjesi\bin\Release\net9.0-windows"
$dst = "c:\Users\Administrator\Desktop\Projes\PosProjesi\release"

# Clean release folder
if (Test-Path $dst) { Remove-Item "$dst\*" -Recurse -Force }

# Copy ALL root-level files (excluding pdb)
Get-ChildItem "$src\*" -File | Where-Object { $_.Extension -ne ".pdb" } | ForEach-Object {
    [System.IO.File]::Copy($_.FullName, "$dst\$($_.Name)", $true)
    Write-Host "Copied $($_.Name)"
}

# Copy Windows native runtimes
$winRuntimes = @(
    "runtimes\win-x64\native",
    "runtimes\win-x86\native",
    "runtimes\win-arm64\native"
)
foreach ($rt in $winRuntimes) {
    $rtSrc = "$src\$rt"
    $rtDst = "$dst\$rt"
    if (Test-Path $rtSrc) {
        if (-not (Test-Path $rtDst)) { New-Item -ItemType Directory -Path $rtDst -Force | Out-Null }
        Get-ChildItem "$rtSrc\*" -File | ForEach-Object {
            [System.IO.File]::Copy($_.FullName, "$rtDst\$($_.Name)", $true)
            Write-Host "Copied $rt\$($_.Name)"
        }
    }
}

Write-Host "`nAll files copied!"
