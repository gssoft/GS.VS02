# Имя файла лога
$logFile = "ConversionLog.txt"

function Write-ConversionLog {
    param (
        [string]$Status,
        [string]$InputName,
        [string]$OutputName,
        [string]$Message,
        [TimeSpan]$Elapsed # Добавили параметр для времени
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    $logEntry = @(
        "$timestamp [$Status]",
        "In : $InputName",
        "Out: $OutputName",
        "Msg: $Message",
        "Time : " + ("{0:mm\:ss\.ff}" -f $Elapsed), # Форматируем время как мм:сс.доли
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

    # Создаем и запускаем секундомер перед началом конвертации
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        ffmpeg -hide_banner -nostats -i $inputPath -map_metadata 0 -codec:a libmp3lame -b:a 320k $outputPath
        
        if ($LASTEXITCODE -eq 0) {
            $stopwatch.Stop() # Останавливаем секундомер при успехе
            
            Write-Host "Ok : $($file.Name)"
            Write-ConversionLog -Status "OK" `
                -InputName $file.Name `
                -OutputName (Split-Path $outputPath -Leaf) `
                -Message "Конвертация успешна." `
                -Elapsed $stopwatch.Elapsed # Передаем замеленное время в лог
        }
        else {
            $stopwatch.Stop()
            throw "FFmpeg вернул код ошибки: $LASTEXITCODE"
        }
    }
    catch {
        $stopwatch.Stop() # Останавливаем секундомер при ошибке
        
        Write-Host "Err: $($file.Name) - $_" -ForegroundColor Red
        Write-ConversionLog -Status "ERROR" `
            -InputName $file.Name `
            -OutputName (Split-Path $outputPath -Leaf) `
            -Message $_.Exception.Message `
            -Elapsed $stopwatch.Elapsed
    }
}

Write-Host "`nГотово. Подробный отчет сохранен в '$logFile'"