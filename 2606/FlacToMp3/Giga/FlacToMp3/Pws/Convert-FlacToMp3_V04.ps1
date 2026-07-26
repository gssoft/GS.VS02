# Имя файла лога
$logFile = "ConversionLog.txt"

function Write-ConversionLog {
    param (
        [string]$Status,
        [string]$InputName,
        [string]$InputSizeStr, # Теперь принимаем готовую строку размера
        [string]$OutputName,
        [string]$OutputSizeStr,
        [string]$ElapsedString,
        [string]$Message
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    $logEntry = @(
        "$timestamp [$Status]",
        "In : $InputName",
        "   Size: $InputSizeStr",      # Просто вставляем готовую строку
        "Out: $OutputName",
        "   Size: $OutputSizeStr",     # Просто вставляем готовую строку
        "Msg: $Message",
        "Time : $ElapsedString",
        "----------------------------------------"
    ) -join "`r`n"
    
    Add-Content -Path $logFile -Value $logEntry -Encoding UTF8
}

Write-Host "Поиск FLAC-файлов в текущем каталоге..."
$flacFiles = Get-ChildItem -Filter *.flac -File

if ($flacFiles.Count -eq 0) {
    Write-Warning "Файлы .flac не найдены."
    exit
}

foreach ($file in $flacFiles) {
    $inputPath = $file.FullName
    $outputPath = $file.BaseName + ".mp3"
    
    # --- БЛОК ПОДГОТОВКИ ДАННЫХ ---
    # Форматируем размеры СРАЗУ здесь, передавая в функцию только готовые строки
    $inSizeStr = "{0:N2} MB" -f ($file.Length / 1MB)
    $outSizeStr = "N/A"

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        ffmpeg -hide_banner -nostats -i $inputPath -map_metadata 0 -codec:a libmp3lame -b:a 320k $outputPath
        
        if ($LASTEXITCODE -eq 0) {
            $stopwatch.Stop()
            
            if (Test-Path $outputPath) {
                $outSizeBytes = (Get-Item $outputPath).Length
                $outSizeStr = "{0:N2} MB" -f ($outSizeBytes / 1MB)
            }
            
            Write-Host "Ok : $($file.Name)"
            Write-ConversionLog -Status "OK" `
                -InputName $file.Name `
                -InputSizeStr $inSizeStr `
                -OutputName (Split-Path $outputPath -Leaf) `
                -OutputSizeStr $outSizeStr `
                -ElapsedString ("{0:mm\:ss\.ff}" -f $stopwatch.Elapsed) `
                -Message "Конвертация успешна."
        }
        else {
            $stopwatch.Stop()
            throw "FFmpeg вернул код ошибки: $LASTEXITCODE"
        }
    }
    catch {
        $stopwatch.Stop()
        
        if (Test-Path $outputPath) {
            $outSizeBytes = (Get-Item $outputPath).Length
            $outSizeStr = "{0:N2} MB" -f ($outSizeBytes / 1MB)
        }
        
        Write-Host "Err: $($file.Name) - $_" -ForegroundColor Red
        Write-ConversionLog -Status "ERROR" `
            -InputName $file.Name `
            -InputSizeStr $inSizeStr `
            -OutputName (Split-Path $outputPath -Leaf) `
            -OutputSizeStr $outSizeStr `
            -ElapsedString ("{0:mm\:ss\.ff}" -f $stopwatch.Elapsed) `
            -Message $_.Exception.Message
    }
}