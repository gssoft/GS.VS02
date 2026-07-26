# Имя файла лога
$logFile = "ConversionLog.txt"

function Write-ConversionLog {
    param (
        [string]$Status,
        [string]$InputName,
        [string]$InputSizeStr,
        [string]$OutputName,
        [string]$OutputSizeStr,
        [string]$ElapsedString,
        [string]$Message
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    $logEntry = @(
        "$timestamp [$Status]",
        "In : $InputName",
        "   Size: $InputSizeStr",
        "Out: $OutputName",
        "   Size: $OutputSizeStr",
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
    
    # Для входящего файла сразу готовим строку
    $inSizeStr = "{0:N2} MB" -f ($file.Length / 1MB)
    
    # Для исходящего задаем ЧИСЛОВОЙ флаг -1 (чтобы отличить от реального нуля)
    $outSizeBytes = -1 

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        ffmpeg -hide_banner -nostats -i $inputPath -map_metadata 0 -codec:a libmp3lame -b:a 320k $outputPath
        
        if ($LASTEXITCODE -eq 0) {
            $stopwatch.Stop()
            
            # --- БЛОК ПРОВЕРКИ ДОСТУПНОСТИ И РАЗМЕРА ---
            $maxRetries = 10
            $retryDelayMs = 200
            $fileReady = $false

            for ($i = 1; $i -le $maxRetries; $i++) {
                try {
                    $stream = [System.IO.File]::Open($outputPath, 'Open', 'Read', 'None')
                    
                    # Если файл открылся, СРАЗУ запрашиваем длину через сам поток
                    # Это самый надежный способ получить актуальный размер без закрытия потока
                    $outSizeBytes = $stream.Length 
                    
                    $stream.Close()
                    $fileReady = $true
                    break
                }
                catch {
                    Start-Sleep -Milliseconds $retryDelayMs
                }
            }
            # ==========================================

            # Форматируем размеры ТОЛЬКО здесь, передавая в функцию чистые строки
            $finalInSizeStr = $inSizeStr
            $finalOutSizeStr = "N/A"
            if ($outSizeBytes -ge 0) { # Если размер был успешно получен (не равен -1)
                $finalOutSizeStr = "{0:N2} MB" -f ($outSizeBytes / 1MB)
            }
            
            Write-Host "Ok : $($file.Name)"
            Write-ConversionLog -Status "OK" `
                -InputName $file.Name `
                -InputSizeStr $finalInSizeStr `
                -OutputName (Split-Path $outputPath -Leaf) `
                -OutputSizeStr $finalOutSizeStr `
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
        
        # В случае ошибки тоже пытаемся считать размер тем же способом
        $maxRetries = 10
        $retryDelayMs = 200
        $fileReady = $false
        $errorOutSize = -1
        for ($i = 1; $i -le $maxRetries; $i++) {
            try { 
                $s=[System.IO.File]::Open($outputPath,'Open','Read','None'); 
                $errorOutSize=$s.Length; 
                $s.Close(); 
                $fileReady=$true; 
                break 
            } 
            catch { Start-Sleep -Milliseconds $retryDelayMs }
        }
        
        $finalInSizeStr = $inSizeStr
        $finalOutSizeStr = "N/A"
        if ($errorOutSize -ge 0) { $finalOutSizeStr = "{0:N2} MB"-f($errorOutSize/1MB) }
        
        Write-Host "Err: $($file.Name) - $_" -ForegroundColor Red
        Write-ConversionLog -Status "ERROR" `
            -InputName $file.Name `
            -InputSizeStr $finalInSizeStr `
            -OutputName (Split-Path $outputPath -Leaf) `
            -OutputSizeStr $finalOutSizeStr `
            -ElapsedString ("{0:mm\:ss\.ff}" -f $stopwatch.Elapsed) `
            -Message $_.Exception.Message
    }
}
