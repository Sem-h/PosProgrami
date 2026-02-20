$src = "c:\Users\Administrator\Desktop\Projes\PosProjesi\PosProjesi\bin\Release\net9.0-windows"
$dst = "c:\Users\Administrator\Desktop\Projes\PosProjesi\release"
$files = @("Dapper.dll","ExCSS.dll","Microsoft.Data.Sqlite.dll","SQLitePCLRaw.batteries_v2.dll","SQLitePCLRaw.core.dll","SQLitePCLRaw.provider.e_sqlite3.dll","Svg.dll","PosProjesi.dll","PosProjesi.exe","PosProjesi.deps.json","PosProjesi.runtimeconfig.json")
foreach ($f in $files) {
    [System.IO.File]::Copy("$src\$f", "$dst\$f", $true)
    Write-Host "Copied $f"
}
Write-Host "All done!"
