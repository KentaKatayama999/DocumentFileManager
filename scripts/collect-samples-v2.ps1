$userProfile = $env:USERPROFILE
$searchPaths = @(
    "$userProfile\Documents",
    "$userProfile\Downloads",
    "$userProfile\Desktop",
    "$userProfile\Pictures"
)

$destBase = 'D:\一時避難所\02_プログラム関連\CSharp\DocumentFileManager\test-files'

$fileTypes = @{
    '.pdf' = 'pdf'
    '.png' = 'images'
    '.jpg' = 'images'
    '.jpeg' = 'images'
    '.bmp' = 'images'
    '.gif' = 'images'
    '.txt' = 'text'
    '.log' = 'text'
    '.csv' = 'text'
    '.md' = 'text'
    '.msg' = 'email'
    '.eml' = 'email'
    '.docx' = 'office'
    '.doc' = 'office'
    '.xlsx' = 'office'
    '.xls' = 'office'
    '.pptx' = 'office'
    '.ppt' = 'office'
    '.3dm' = 'cad'
    '.sldprt' = 'cad'
    '.sldasm' = 'cad'
    '.dwg' = 'cad'
}

# Create destination directories
$subdirs = @('pdf', 'images', 'text', 'email', 'office', 'cad', 'other')
foreach ($subdir in $subdirs) {
    $path = Join-Path $destBase $subdir
    if (-not (Test-Path $path)) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
    }
}

$results = @()
$successCount = 0

Write-Host "Searching and copying files..."
Write-Host ""

foreach ($ext in $fileTypes.Keys) {
    $copied = $false

    foreach ($path in $searchPaths) {
        if (Test-Path $path) {
            $file = Get-ChildItem -Path $path -Filter "*$ext" -Recurse -ErrorAction SilentlyContinue -File |
                     Where-Object { $_.Length -lt 100MB -and $_.FullName -notmatch 'node_modules|\.git|AppData|\\bin\\|\\obj\\' } |
                     Select-Object -First 1

            if ($file) {
                $destDir = Join-Path $destBase $fileTypes[$ext]
                $destFile = Join-Path $destDir "sample$ext"

                try {
                    # Ensure source file is readable
                    if (-not (Test-Path $file.FullName -PathType Leaf)) {
                        continue
                    }

                    # Copy file
                    Copy-Item -Path $file.FullName -Destination $destFile -Force -ErrorAction Stop

                    # Verify copy
                    if (Test-Path $destFile -PathType Leaf) {
                        $copiedFile = Get-Item $destFile
                        $sizeKB = [math]::Round($copiedFile.Length / 1KB, 2)
                        Write-Host "[OK] $ext : $($file.Name) ($sizeKB KB) -> $destFile"
                        $results += "[OK] $ext"
                        $successCount++
                        $copied = $true
                        break
                    } else {
                        Write-Host "[FAIL] $ext : Copy failed - destination file not created"
                    }
                }
                catch {
                    Write-Host "[ERROR] $ext : $($_.Exception.Message)"
                }
            }
        }
    }

    if (-not $copied) {
        Write-Host "[FAIL] $ext : Not found or copy failed"
        $results += "[FAIL] $ext"
    }
}

Write-Host ""
Write-Host "=== Summary ==="
Write-Host "Successfully copied: $successCount / $($fileTypes.Count) files"
Write-Host ""
Write-Host "Verifying copied files..."
$copiedFiles = Get-ChildItem -Path $destBase -Recurse -File
Write-Host "Total files in test-files: $($copiedFiles.Count)"
