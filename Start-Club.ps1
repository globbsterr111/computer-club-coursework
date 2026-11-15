$ErrorActionPreference = 'Stop'

function Open-DatabaseConnection($settings) {
    $connection = New-Object System.Data.SqlClient.SqlConnection($settings.ConnectionString)
    try {
        $connection.Open()
        return $connection
    } catch {
        $connection.Dispose()
        throw
    }
}

function Initialize-ClubDatabase($settings, $master) {
    $databaseName = $settings.InitialCatalog

    # The master connection keeps the application lock until initialization is finished.
    $create = $master.CreateCommand()
    try {
        $create.CommandText = @"
DECLARE @result int;
EXEC @result = sys.sp_getapplock
    @Resource = N'ComputerClub.Setup', @LockMode = 'Exclusive',
    @LockOwner = 'Session', @LockTimeout = 15000;
IF @result < 0 THROW 51000, N'Подготовка базы уже выполняется. Повторите запуск позже.', 1;
IF DB_ID(N'computer_club_db') IS NULL EXEC(N'CREATE DATABASE [computer_club_db]');
"@
        [void]$create.ExecuteNonQuery()
    } finally {
        $create.Dispose()
    }

    $database = Open-DatabaseConnection $settings
    try {
        $check = $database.CreateCommand()
        try {
            $check.CommandText = @"
SELECT COUNT(*)
FROM sys.objects
WHERE schema_id = SCHEMA_ID(N'dbo') AND (
    (type = 'U' AND name IN (N'roles', N'computer_statuses', N'session_statuses',
        N'payment_methods', N'clients', N'users', N'computers', N'tariffs', N'sessions', N'payments'))
    OR (type = 'V' AND name = N'v_session_costs')
    OR (type = 'P' AND name IN (N'start_session', N'finish_session', N'pay_session'))
    OR (type = 'TR' AND name = N'tr_tariff_history')
);
"@
            if ([int]$check.ExecuteScalar() -eq 15) { return }
            $check.CommandText = @"
SELECT COUNT(*) FROM sys.objects
WHERE is_ms_shipped = 0 AND type IN ('U', 'V', 'P', 'TR', 'FN', 'IF', 'TF');
"@
            if ([int]$check.ExecuteScalar() -gt 0) {
                throw "База $databaseName уже содержит объекты, но её структура отличается от проекта. Данные не изменены."
            }
        } finally {
            $check.Dispose()
        }

        Write-Host 'Подготавливаю таблицы и демонстрационные данные...'
        # The scripts contain GO separators and one balanced inner transaction.
        # The outer transaction keeps both scripts atomic.
        $transaction = $database.BeginTransaction()
        try {
            foreach ($file in @('01_БД.sql', '02_Сеансы.sql')) {
                $sql = Get-Content -LiteralPath (Join-Path "$PSScriptRoot/SQL" $file) -Raw -Encoding UTF8
                foreach ($batch in ($sql -split '(?im)^\s*GO[ \t]*(?:--[^\r\n]*)?\r?$')) {
                    if ([string]::IsNullOrWhiteSpace($batch)) { continue }
                    $command = $database.CreateCommand()
                    try {
                        $command.Transaction = $transaction
                        $command.CommandText = $batch
                        [void]$command.ExecuteNonQuery()
                    } finally {
                        $command.Dispose()
                    }
                }
            }
            $transaction.Commit()
        } finally {
            $transaction.Dispose()
        }
    } finally {
        $database.Dispose()
    }
}

Set-Location -LiteralPath $PSScriptRoot

try {
    Write-Host 'Компьютерный клуб'
    $dotnet = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if (-not $dotnet -or -not (& $dotnet.Source --list-sdks | Where-Object { $_ -match '^10\.' })) {
        throw 'Установите .NET 10 SDK, затем снова откройте файл запуска.'
    }

    $connectionFile = Join-Path $PSScriptRoot 'connection.txt'
    $connectionString = $env:CLUB_CONNECTION
    if (-not $connectionString -and (Test-Path -LiteralPath $connectionFile -PathType Leaf)) {
        $connectionString = (Get-Content -LiteralPath $connectionFile -Raw -Encoding UTF8).Trim()
    }
    $customConnection = -not [string]::IsNullOrWhiteSpace($connectionString)
    if (-not $customConnection) {
        $connectionString = (Get-Content -LiteralPath "$PSScriptRoot/connection.example.txt" -Raw -Encoding UTF8).Trim()
    }
    $settings = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($connectionString)
    if ($settings.InitialCatalog -ne 'computer_club_db') {
        throw 'Укажите базу computer_club_db в настройках подключения.'
    }
    $settings['Connect Timeout'] = 4
    $servers = @($settings.DataSource)
    if (-not $customConnection) {
        $servers += @('.', '(localdb)\MSSQLLocalDB')
    }

    $master = $null
    foreach ($server in $servers) {
        $settings['Data Source'] = $server
        $masterSettings = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($settings.ConnectionString)
        $masterSettings['Initial Catalog'] = 'master'
        $masterSettings['Pooling'] = $false
        try {
            $master = Open-DatabaseConnection $masterSettings
            break
        } catch {
            if ($customConnection) { throw "Не удалось подключиться к SQL Server «$server». Проверьте настройки и доступ." }
        }
    }
    if (-not $master) {
        throw 'SQL Server недоступен. Запустите службу SQL Server (SQLEXPRESS) или укажите свой сервер в connection.txt.'
    }

    try {
        Write-Host 'Собираю приложение. При первом запуске могут скачиваться зависимости...'
        & $dotnet.Source build Club.csproj --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Не удалось собрать приложение. Сведения об ошибке приведены выше.' }
        Initialize-ClubDatabase $settings $master
    } finally {
        $master.Dispose()
    }

    $env:CLUB_CONNECTION = $settings.ConnectionString
    & $dotnet.Source run --no-build --project Club.csproj
    if ($LASTEXITCODE -ne 0) { throw 'Приложение завершилось с ошибкой.' }
} catch {
    Write-Host ''
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
