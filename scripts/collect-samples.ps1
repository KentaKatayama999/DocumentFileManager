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

# Create destination directories if not exist
$subdirs = @('pdf', 'images', 'text', 'email', 'office', 'cad', 'other')
foreach ($subdir in $subdirs) {
    $path = Join-Path $destBase $subdir
    if (-not (Test-Path $path)) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
    }
}

$results = @()

Write-Host "Searching in:"
foreach ($p in $searchPaths) {
    if (Test-Path $p) {
        Write-Host "  [OK] $p"
    } else {
        Write-Host "  [SKIP] $p (not found)"
    }
}
Write-Host ""

foreach ($ext in $fileTypes.Keys) {
    $found = $false
    foreach ($path in $searchPaths) {
        if (Test-Path $path) {
            $file = Get-ChildItem -Path $path -Filter "*$ext" -Recurse -ErrorAction SilentlyContinue -File |
                     Where-Object { $_.Length -lt 100MB -and $_.FullName -notmatch 'node_modules|\.git|AppData|\\bin\\|\\obj\\' } |
                     Select-Object -First 1

            if ($file) {
                $destDir = Join-Path $destBase $fileTypes[$ext]
                $destFile = Join-Path $destDir "sample$ext"

                # Ensure destination directory exists
                if (-not (Test-Path $destDir)) {
                    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
                }

                try {
                    Copy-Item -Path $file.FullName -Destination $destFile -Force -ErrorAction Stop
                    $sizeKB = [math]::Round($file.Length / 1KB, 2)
                    $results += "[OK] $ext : $($file.Name) ($sizeKB KB)"
                    $found = $true
                    break
                }
                catch {
                    Write-Host "[DEBUG] Failed to copy $($file.FullName) to $destFile : $_"
                }
            }
        }
    }

    if (-not $found) {
        $results += "[FAIL] $ext : Not found"
    }
}

Write-Host ""
Write-Host "=== Sample File Collection Results ==="
Write-Host ""
$results | ForEach-Object { Write-Host $_ }

$successCount = ($results | Where-Object { $_ -match "^\[OK\]" }).Count
$totalCount = $fileTypes.Count
Write-Host ""
Write-Host "Completed: $successCount / $totalCount files"
