# Convert-FlacToMp3_v2_compat.ps1
# Версия для старых систем (Windows PowerShell 5.1), заменяющая 'using' на .Dispose()

param (
    [string]$LogFile = "ConversionLog.txt",
    [string]$ffmpegPath = "ffmpeg", 
    [string]$ffprobePath = "ffprobe"
)

function Write-ConversionLog {
    param (
        [string]$Status,
        [string]$InputName,
        [long]$InputSizeBytes,
        [string]$OutputName,
        [long]$OutputSizeBytes,
        [timespan]$Elapsed,
        [string]$Message
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    function Format-BytesAsKb {
        param ([long]$size)
        if ($size -eq -1) { return "N/A" }
        return ("{0:N2} KB" -f ($size / 1KB))
    }

    $logEntry = @(
        "$timestamp [$Status]",
        "In : $InputName",
        "   Size: $(Format-BytesAsKb $InputSizeBytes)",
        "Out: $OutputName",
        "   Size: $(Format-BytesAsKb $OutputSizeBytes)",
        "Msg: $Message",
        "Time : {0:mm\:ss\.ff}" -f $Elapsed,
        "----------------------------------------"
    ) -join "`r`n"
    
    Add-Content -Path $LogFile -Value $logEntry -Encoding UTF8
}

function Wait-FileReady {
    param (
        [string]$Path,
        [int]$MaxRetries = 10,
        [int]$DelayMs = 200
    )
    for ($i = 1; $i -le $MaxRetries; $i++) {
        $stream = $null
        try {
            $stream = [System.IO.File]::Open($Path, 'Open', 'Read', 'None')
            $length = $stream.Length
            return $length
        } catch {
            Start-Sleep -Milliseconds $DelayMs
        } finally {
            if ($stream) { $stream.Dispose() }
        }
    }
    throw "Файл '$Path' не освободился после $MaxRetries попыток."
}

function Convert-SingleFlacToMp3 {
    param (
        [System.IO.FileInfo]$File,
        [string]$ffmpegPath,
        [string]$ffprobePath,
        [ref]$OkCount,
        [ref]$ErrCount,
        [ref]$SkipCount
    )

    $inputPath = $File.FullName
    $outputPath = Join-Path $File.DirectoryName ("$($File.BaseName).mp3")
    $inSizeBytes = $File.Length

    # Блок пропуска существующих файлов
    if ((Test-Path $outputPath)) {
        $existingItem = Get-Item $outputPath
        if ($existingItem.Length -gt 0) {
            Write-Host "Skip: $($File.Name)"
            Write-ConversionLog -Status "SKIP" `
                -InputName $File.Name `
                -InputSizeBytes $inSizeBytes `
                -OutputName $existingItem.Name `
                -OutputSizeBytes $existingItem.Length `
                -Elapsed ([TimeSpan]::Zero) `
                -Message "Файл уже существует."
            $SkipCount.Value++
            return
        }
    }

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        & $ffmpegPath -hide_banner -nostats -i $inputPath -map_metadata 0 -codec:a libmp3lame -b:a 320k $outputPath
        
        if ($LASTEXITCODE -eq 0) {
            $stopwatch.Stop()
            
            $outputSizeBytes = Wait-FileReady -Path $outputPath
            
            [int]$actualBitrate = 0
            try {
                $rawProbe = & $ffprobePath -v error -select_streams a:0 -show_entries stream=bit_rate -of default=noprint_wrappers=1:nokey=1 $outputPath 2>$null
                
                if ($rawProbe -and $rawProbe.Trim() -ne 'N/A') {
                    [int]$actualBitrate = [math]::Round([long]$rawProbe / 1000)
                } else {
                    $durationSecStr = & $ffprobePath -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 $outputPath 2>$null
                    if ($durationSecStr -and $outputSizeBytes -gt 0) {
                        $calcBps = ([double]$outputSizeBytes * 8) / [double]$durationSecStr
                        [int]$actualBitrate = [math]::Round($calcBps / 1000)
                    } 
                }
            } catch {}

            $statusType = "OK"
            $msgSuffix = ""
            if ($actualBitrate -lt 315 -or $actualBitrate -gt 325) {
                $statusType = "OK-WARN"
                $msgSuffix = " ВНИМАНИЕ: Битрейт $actualBitrate kbps!"
                Write-Host "Warn: Bitrate is $actualBitrate kbps for $($File.Name)" -ForegroundColor Yellow
            } else {
                Write-Host "Ok : $($File.Name)"
            }

            Write-ConversionLog -Status $statusType `
                -InputName $File.Name `
                -InputSizeBytes $inSizeBytes `
                -OutputName (Split-Path $outputPath -Leaf) `
                -OutputSizeBytes $outputSizeBytes `
                -Elapsed $stopwatch.Elapsed `
                -Message ("Конвертация успешна." + $msgSuffix)
            
            $OkCount.Value++
        } else {
            $stopwatch.Stop()
            throw "FFmpeg вернул код ошибки: $LASTEXITCODE"
        }
    } catch {
        $stopwatch.Stop()
        
        $errorOutSize = -1
        try {
            $errorOutSize = Wait-FileReady -Path $outputPath
        } catch {}

        Write-Host "Err: $($File.Name) - $_" -ForegroundColor Red
        Write-ConversionLog -Status "ERROR" `
            -InputName $File.Name `
            -InputSizeBytes $inSizeBytes `
            -OutputName (Split-Path $outputPath -Leaf) `
            -OutputSizeBytes $errorOutSize `
            -Elapsed $stopwatch.Elapsed `
            -Message $_.Exception.Message
        $ErrCount.Value++
    }
}

# --- ОСНОВНОЙ БЛОК ---
Clear-Host
Write-Host "Поиск FLAC-файлов (включая подпапки)..."
$flacFiles = Get-ChildItem -Filter *.flac -File -Recurse -ErrorAction SilentlyContinue

if (-not $flacFiles) {
    Write-Warning "Файлы .flac не найдены."
    exit
}

[int]$total = $flacFiles.Count
[int]$okCount = 0
[int]$errCount = 0
[int]$skipCount = 0

foreach ($file in $flacFiles) {
    Convert-SingleFlacToMp3 -File $file `
        -ffmpegPath $ffmpegPath `
        -ffprobePath $ffprobePath `
        -OkCount ([ref]$okCount) `
        -ErrCount ([ref]$errCount) `
        -SkipCount ([ref]$skipCount)
}

Write-Host "`n================== ПРОТОКОЛ ЗАВЕРШЕН =================="
Write-Host "Всего обработано файлов: $total"
Write-Host "Успешно конвертировано:  $okCount"
Write-Host "Пропущено (существовали): $skipCount"
Write-Host "Ошибок:                  $errCount"
Write-Host "======================================================="
Write-Host "Подробный отчет сохранен в '$LogFile'"
