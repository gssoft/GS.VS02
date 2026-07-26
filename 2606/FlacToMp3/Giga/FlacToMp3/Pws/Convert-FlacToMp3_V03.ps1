# Имя файла лога
$logFile = "ConversionLog.txt"

function Write-ConversionLog {
    param (
        [string]$Status,
        [string]$InputName,
        [long]$InputSizeBytes, 
        [string]$OutputName,
        [long]$OutputSizeBytes,
        [string]$ElapsedString, # Теперь принимаем готовую строку времени
        [string]$Message
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    # Функция для красивого перевода байтов в МБ (например, 51840 KiB -> 50.62 MB)
    function Format-FileSize {
        param ([long]$size)
        if ($size -eq -1) { return "N/A" }
        return ("{0:N2} MB" -f ($size / 1MB))
    }

    $logEntry = @(
        "$timestamp [$Status]",
        "In : $InputName",
        "   Size: " + (Format-FileSize $InputSizeBytes),
        "Out: $OutputName",
        "   Size: " + (Format-FileSize $OutputSizeBytes),
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
    
    # Размеры до обработки
    $inSize = $file.Length 
    $outSize = -1 # Значение по умолчанию, если файл не создался

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        ffmpeg -hide_banner -nostats -i $inputPath -map_metadata 0 -codec:a libmp3lame -b:a 320k $outputPath
        
        if ($LASTEXITCODE -eq 0) {
            $stopwatch.Stop()
            
            # Проверяем существование и получаем размер выходного файла ПОСЛЕ завершения FFmpeg
            if (Test-Path $outputPath) {
                $outSize = (Get-Item $outputPath).Length
            }
            
            Write-Host "Ok : $($file.Name)"
            Write-ConversionLog -Status "OK" `
                -InputName $file.Name `
                -InputSizeBytes $inSize `
                -OutputName (Split-Path $outputPath -Leaf) `
                -OutputSizeBytes $outSize `
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
        
        # Пытаемся получить размер даже битого/недописанного MP3
        if (Test-Path $outputPath) {
            $outSize = (Get-Item $outputPath).Length
        }
        
        Write-Host "Err: $($file.Name) - $_" -ForegroundColor Red
        Write-ConversionLog -Status "ERROR" `
            -InputName $file.Name `
            -InputSizeBytes $inSize `
            -OutputName (Split-Path $outputPath -Leaf) `
            -OutputSizeBytes $outSize `
            -ElapsedString ("{0:mm\:ss\.ff}" -f $stopwatch.Elapsed) `
            -Message $_.Exception.Message
    }
}