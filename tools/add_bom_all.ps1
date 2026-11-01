$files = Get-ChildItem -Path 'D:\DSHWorkSpace\manosaba_a11y' -Recurse -Include *.ps1 -File | Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\package\\BepInEx' }
foreach ($f in $files) {
    $b = [IO.File]::ReadAllBytes($f.FullName)
    if ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF) { continue }
    [IO.File]::WriteAllBytes($f.FullName, [byte[]](0xEF,0xBB,0xBF) + $b)
    Write-Host "BOM added: $($f.Name)"
}
