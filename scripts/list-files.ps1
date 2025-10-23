Get-ChildItem 'D:\一時避難所\02_プログラム関連\CSharp\DocumentFileManager\test-files' -Recurse -File |
    Select-Object DirectoryName, Name, @{Name='Size(KB)';Expression={[math]::Round($_.Length/1KB,2)}} |
    Format-Table -AutoSize
