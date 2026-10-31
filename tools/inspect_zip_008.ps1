Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath = 'D:\DSHWorkSpace\manosaba_a11y\mod\dist\ManosabaA11y-0.0.0.8.zip'
$z = [IO.Compression.ZipFile]::OpenRead($zipPath)
Write-Host ("entries=" + $z.Entries.Count)
$z.Entries | Where-Object { $_.FullName -match 'README|安装说明|常见问题|ManosabaA11y\.dll|changelog' } |
    ForEach-Object { Write-Host ($_.FullName + '  ' + $_.Length) }
$bad = @($z.Entries | Where-Object { $_.FullName.Contains('\') })
Write-Host ("backslash_entries=" + $bad.Count)
$z.Dispose()
