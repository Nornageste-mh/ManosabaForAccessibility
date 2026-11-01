$tokens = $null; $errors = $null
[void][System.Management.Automation.Language.Parser]::ParseFile("D:\DSHWorkSpace\manosaba_a11y\tests\Suite.ps1", [ref]$tokens, [ref]$errors)
if ($errors.Count -gt 0) { $errors[0..1] | ForEach-Object { $_.Message } } else { "syntax OK" }
