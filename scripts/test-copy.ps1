$testSrc = Get-ChildItem -Path 'C:\Users\ken-katayama\Documents' -Filter '*.pdf' -Recurse -ErrorAction SilentlyContinue -File | Select-Object -First 1

if ($testSrc) {
    Write-Host "Source file found: $($testSrc.FullName)"
    $testDest = 'D:\一時避難所\02_プログラム関連\CSharp\DocumentFileManager\test-files\pdf\sample.pdf'
    Write-Host "Destination: $testDest"

    try {
        Copy-Item -Path $testSrc.FullName -Destination $testDest -Force -Verbose -ErrorAction Stop
        Write-Host "Copy command executed"

        if (Test-Path $testDest) {
            Write-Host "Copy successful!"
            $file = Get-Item $testDest
            Write-Host "File size: $($file.Length) bytes"
        } else {
            Write-Host "Copy failed - file does not exist at destination"
        }
    }
    catch {
        Write-Host "Copy failed with error: $_"
    }
} else {
    Write-Host "No PDF found in Documents"
}
