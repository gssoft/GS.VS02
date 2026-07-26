# Имя файла лога (создастся в той же папке, где лежит скрипт)
$logFile = "ConversionLog.txt"

# Функция для записи блока данных в лог-файл
function Write-ConversionLog {
    param (
        [string]$Status,
        [string]$InputName,
        [string]$OutputName,
        [string]$Message
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    # Формируем блок текста согласно вашему пожеланию: дата -> статус -> вход над выходом -> сообщение
    $logEntry = @(
        "$timestamp [$Status]",
        "In : $InputName",
        "Out: $OutputName",
        "Msg: $Message",
        "----------------------------------------"
    ) -join "`r`n"
    
    # Добавляем (-Append) запись в файл UTF8 без BOM
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

    try {
        # Запускаем FFmpeg. -nostats скрывает бегущую строку прогресса, чтобы не засорять вывод.
        ffmpeg -hide_banner -nostats -i $inputPath -map_metadata 0 -codec:a libmp3lame -b:a 320k $outputPath
        
        # Проверяем код возврата последней команды ($LASTEXITCODE). У FFmpeg 0 — успех.
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Ok : $($file.Name)"
            Write-ConversionLog -Status "OK" -InputName $file.Name -OutputName (Split-Path $outputPath -Leaf) -Message "Конвертация успешна."
        }
        else {
            throw "FFmpeg вернул код ошибки: $LASTEXITCODE"
        }
    }
    catch {
        Write-Host "Err: $($file.Name) - $_" -ForegroundColor Red
        Write-ConversionLog -Status "ERROR" -InputName $file.Name -OutputName (Split-Path $outputPath -Leaf) -Message $_.Exception.Message
    }
}

Write-Host "`nГотово. Подробный отчет сохранен в '$logFile'"