$server = "103.180.120.159,1433"
$database = "DeliveryDost_Dev"
$user = "mcp_deliverydost_login"
$password = 'J7@vR#9pW!d83ZqT$mK2^HsxL8&FbN1Q'

$script1 = "C:\Users\HP\Desktop\finnidTech\database\migrations\001_CreateAuthTables.sql"
$script2 = "C:\Users\HP\Desktop\finnidTech\database\migrations\002_SeedDefaultAdmin.sql"

Write-Host "Running SQL Migrations..." -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/2] Creating Auth Tables..." -ForegroundColor Yellow
& sqlcmd -S $server -U $user -P $password -d $database -i $script1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create tables" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "[2/2] Seeding Default Data..." -ForegroundColor Yellow
& sqlcmd -S $server -U $user -P $password -d $database -i $script2
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to seed data" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "All migrations completed successfully!" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
