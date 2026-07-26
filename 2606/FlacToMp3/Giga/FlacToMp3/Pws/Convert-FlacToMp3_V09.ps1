# Полный код скрипта Convert-FlacToMp3.ps1 (Финальная версия)
# Включает: логирование (в Килобайтах), защиту от повторного запуска,
# проверку битрейта итогового файла через ffprobe и финальную статистику.

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
    
    # Функция форматирования размера. Возвращает строку вида "422 664.00 KB"
    function Format-FileSize {
        param ([long]$size)
        if ($size -eq -1) { return "N/A" }
        return ("{0:N2} KB" -f ($size / 1KB))
    }

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

# Счетчики для итоговой статистики
$total = $flacFiles.Count
$okCount = 0
$errCount = 0
$skipCount = 0

foreach ($file in $flacFiles) {
    $inputPath = $file.FullName
    $outputPath = $file.BaseName + ".mp3"
    
    # Подготовка данных входного файла (ТЕПЕРЬ В КБ)
    $inSizeStr = "{0:N2} KB" -f ($file.Length / 1KB)
    $outSizeBytes = -1 

    # --- БЛОК ПРОПУСКА СУЩЕСТВУЮЩИХ ФАЙЛОВ ---
    if ((Test-Path $outputPath) -and ((Get-Item $outputPath).Length -gt 0)) {
        $existingSize = "{0:N2} KB" -f ((Get-Item $outputPath).Length / 1KB)
        Write-Host "Skip: $($file.Name)"
        Write-ConversionLog -Status "SKIP" `
            -InputName $file.Name `
            -InputSizeStr $inSizeStr `
            -OutputName (Split-Path $outputPath -Leaf) `
            -OutputSizeStr $existingSize `
            -ElapsedString "00:00.00" `
            -Message "Файл уже существует."
        $skipCount++
        continue
    }
    # ==========================================

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        ffmpeg -hide_banner -nostats -i $inputPath -map_metadata 0 -codec:a libmp3lame -b:a 320k $outputPath
        
        if ($LASTEXITCODE -eq 0) {
            $stopwatch.Stop()
            
            # --- БЛОК ПРОВЕРКИ ДОСТУПНОСТИ И РАЗМЕРА OUT ---
            $maxRetries = 10
            $retryDelayMs = 200
            $fileReady = $false

            for ($i = 1; $i -le $maxRetries; $i++) {
                try {
                    $stream = [System.IO.File]::Open($outputPath, 'Open', 'Read', 'None')
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

            # Форматируем строку размера Out (ТЕПЕРЬ В КБ)
            $finalOutSizeStr = "N/A"
            if ($outSizeBytes -ge 0) {
                $calcSize = $outSizeBytes / 1KB
                $finalOutSizeStr = "{0:N2} KB" -f $calcSize
            }

            # --- БЛОК ПРОВЕРКИ БИТРЕЙТА (через ffprobe) ---
            [int]$actualBitrate = 0 # Значение по умолчанию на случай сбоя ffprobe
            try {
                $rawProbe = & ffprobe -v error -select_streams a:0 -show_entries stream=bit_rate -of default=noprint_wrappers=1:nokey=1 $outputPath 2>$null
                
                if ($rawProbe -and $rawProbe.Trim() -ne 'N/A') {
                    [int]$actualBitrate = [math]::Round([long]$rawProbe / 1000)
                }
                else {
                    # Резервный расчет: Размер (байты) * 8 / Длительность (сек)
                    $durationSec = (& ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 $outputPath 2>$null)
                    if ($durationSec -and $outSizeBytes -gt 0) {
                        $calcBps = ([double]$outSizeBytes * 8) / [double]$durationSec
                        [int]$actualBitrate = [math]::Round($calcBps / 1000)
                    } 
                }
            }
            catch {
                # Оставляем actualBitrate равным 0, если ffprobe выдал ошибку
            }

            if ($actualBitrate -ne 320) {
                $msgSuffix = " ВНИМАНИЕ: Битрейт $actualBitrate kbps!"
                Write-Host "Warn: Bitrate is $actualBitrate kbps for $($file.Name)" -ForegroundColor Yellow
                Write-ConversionLog -Status "OK-WARN" `
                    -InputName $file.Name `
                    -InputSizeStr $inSizeStr `
                    -OutputName (Split-Path $outputPath -Leaf) `
                    -OutputSizeStr $finalOutSizeStr `
                    -ElapsedString ("{0:mm\:ss\.ff}" -f $stopwatch.Elapsed) `
                    -Message ("Конвертация успешна." + $msgSuffix)
            }
            else {
                Write-Host "Ok : $($file.Name)"
                Write-ConversionLog -Status "OK" `
                    -InputName $file.Name `
                    -InputSizeStr $inSizeStr `
                    -OutputName (Split-Path $outputPath -Leaf) `
                    -OutputSizeStr $finalOutSizeStr `
                    -ElapsedString ("{0:mm\:ss\.ff}" -f $stopwatch.Elapsed) `
                    -Message "Конвертация успешна."
            }
            $okCount++
        }
        else {
            $stopwatch.Stop()
            throw "FFmpeg вернул код ошибки: $LASTEXITCODE"
        }
    }
    catch {
        $stopwatch.Stop()
        
        # Попытка считать размер даже при ошибке конвертации
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
        
        $finalOutSizeStr = "N/A"
        if ($errorOutSize -ge 0) {
            $calcErrSize = $errorOutSize / 1KB
            $finalOutSizeStr = "{0:N2} KB" -f $calcErrSize
        }
        
        Write-Host "Err: $($file.Name) - $_" -ForegroundColor Red
        Write-ConversionLog -Status "ERROR" `
            -InputName $file.Name `
            -InputSizeStr $inSizeStr `
            -OutputName (Split-Path $outputPath -Leaf) `
            -OutputSizeStr $finalOutSizeStr `
            -ElapsedString ("{0:mm\:ss\.ff}" -f $stopwatch.Elapsed) `
            -Message $_.Exception.Message
        $errCount++
    }
}

# --- ИТОГОВАЯ СТАТИСТИКА ---
Write-Host "`n================== ПРОТОКОЛ ЗАВЕРШЕН =================="
Write-Host "Всего обработано файлов: $total"
Write-Host "Успешно конвертировано:  $okCount"
Write-Host "Пропущено (существовали): $skipCount"
Write-Host "Ошибок:                  $errCount"
Write-Host "======================================================="
Write-Host "Подробный отчет сохранен в '$logFile'"