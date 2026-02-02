# PowerShell Script zum Entfernen von Debug-Ausgaben
param(
    [string]$Path = "src"
)

$files = Get-ChildItem -Path $Path -Filter "*.cs" -Recurse
$removedCount = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalLength = ($content | Measure-Object -Character).Characters

    # Entferne Zeilen mit Debug, Console oder Trace WriteLine
    $content = $content -replace '.*\s*System\.Diagnostics\.Debug\.WriteLine\([^)]*\);?\s*\r?\n', "`n"
    $content = $content -replace '.*\s*Console\.WriteLine\([^)]*\);?\s*\r?\n', "`n"
    $content = $content -replace '.*\s*System\.Diagnostics\.Trace\.WriteLine\([^)]*\);?\s*\r?\n', "`n"

    # Entferne mehrfache Zeilenumbrüche
    $content = $content -replace '\n\s*\n\s*\n', "`n`n"

    $newLength = ($content | Measure-Object -Character).Characters

    if ($originalLength -ne $newLength) {
        Set-Content $file.FullName -Value $content -Encoding UTF8
        $removedCount++
        Write-Host "Bereinigt: $($file.Name) ($(($originalLength - $newLength)) Zeichen entfernt)"
    }
}

Write-Host "Fertig! $removedCount Dateien wurden bereinigt."
