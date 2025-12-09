@echo off
echo Running SQL Migrations...
echo.

set SERVER=103.180.120.159,1433
set DATABASE=DeliveryDost_Dev
set USER=mcp_deliverydost_login
set PASS=J7@vR#9pW!d83ZqT$mK2^^HsxL8^&FbN1Q

echo [1/2] Creating Auth Tables...
sqlcmd -S %SERVER% -U %USER% -P "%PASS%" -d %DATABASE% -i "C:\Users\HP\Desktop\finnidTech\database\migrations\001_CreateAuthTables.sql"
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to create tables
    pause
    exit /b 1
)

echo.
echo [2/2] Seeding Default Data...
sqlcmd -S %SERVER% -U %USER% -P "%PASS%" -d %DATABASE% -i "C:\Users\HP\Desktop\finnidTech\database\migrations\002_SeedDefaultAdmin.sql"
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to seed data
    pause
    exit /b 1
)

echo.
echo =============================================
echo All migrations completed successfully!
echo =============================================
pause
