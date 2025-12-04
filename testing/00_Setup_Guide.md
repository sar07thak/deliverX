# DeliverX - Setup & Installation Guide

## How to Run DeliverX from GitHub Download

This guide explains how to set up and run DeliverX after downloading the zip file from GitHub.

---

## Prerequisites

Before starting, install these on your computer:

### 1. .NET SDK 10.0 (or latest)
- Download: https://dotnet.microsoft.com/download
- Verify installation:
  ```bash
  dotnet --version
  ```
  Expected: `10.0.x` or similar

### 2. Node.js (v18 or later)
- Download: https://nodejs.org/
- Verify installation:
  ```bash
  node --version
  npm --version
  ```
  Expected: `v18.x.x` or higher

### 3. Git (Optional - for cloning)
- Download: https://git-scm.com/downloads

---

## Step 1: Download & Extract

### Option A: Download ZIP
1. Go to GitHub repository
2. Click **Code** → **Download ZIP**
3. Extract to a folder (e.g., `C:\Projects\DeliverX` or `~/Projects/DeliverX`)

### Option B: Clone with Git
```bash
git clone https://github.com/your-repo/deliverx.git
cd deliverx
```

---

## Step 2: Project Structure

After extraction, you'll have:
```
DeliverX/
├── src/
│   ├── DeliverX.API/          # Backend API (.NET)
│   ├── DeliverX.Application/  # Business logic
│   ├── DeliverX.Domain/       # Entities
│   └── DeliverX.Infrastructure/ # Database & Services
├── deliverx-ui/               # Frontend (React)
├── testing/                   # Test documentation
├── DeliverX.sln              # Solution file
└── README.md
```

---

## Step 3: Setup Backend API

### 3.1 Open Terminal/Command Prompt
Navigate to the project folder:
```bash
cd C:\Projects\DeliverX
# or on Mac/Linux
cd ~/Projects/DeliverX
```

### 3.2 Restore NuGet Packages
```bash
dotnet restore
```

### 3.3 Build the Solution
```bash
dotnet build
```
Expected output: `Build succeeded. 0 Error(s)`

### 3.4 Navigate to API Project
```bash
cd src/DeliverX.API
```

### 3.5 Run Database Migrations (First Time Only)
```bash
dotnet ef database update
```
This creates the SQLite database (`deliverx.db`)

**Note:** If `dotnet ef` is not found, install it:
```bash
dotnet tool install --global dotnet-ef
```

### 3.6 Start the API
```bash
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5205
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### 3.7 Verify API is Running
Open browser: http://localhost:5205/swagger

You should see the Swagger API documentation page.

**Keep this terminal open!** The API must be running for the UI to work.

---

## Step 4: Setup Frontend UI

### 4.1 Open NEW Terminal/Command Prompt
Keep the API terminal running, open a new one.

### 4.2 Navigate to UI Folder
```bash
cd C:\Projects\DeliverX\deliverx-ui
# or on Mac/Linux
cd ~/Projects/DeliverX/deliverx-ui
```

### 4.3 Install Dependencies
```bash
npm install
```
This downloads all required packages (may take 2-5 minutes).

### 4.4 Start the UI
```bash
npm start
```

Expected output:
```
Compiled successfully!

You can now view deliverx-ui in the browser.

  Local:            http://localhost:3000
  On Your Network:  http://192.168.x.x:3000
```

### 4.5 Access the Application
Browser will automatically open, or go to: http://localhost:3000

---

## Step 5: First-Time Usage

### 5.1 Register Your First User

1. Open http://localhost:3000
2. Select a role:
   - **End Consumer** - For personal deliveries
   - **Business** - For business deliveries
   - **Delivery Partner** - To deliver packages
   - **Admin/Manager** (scroll down) - For DPCM or SuperAdmin

3. Enter phone number (e.g., `9999999999`)
4. Click **Send OTP**
5. Enter the OTP shown in the success message
6. Click **Verify & Login**

### 5.2 Test Phone Numbers (Suggested)

| Role | Phone Number | Purpose |
|------|--------------|---------|
| SuperAdmin | 9000000001 | Platform admin |
| DPCM | 8888800001 | Channel manager |
| DP | 7777700001 | Delivery partner |
| BC | 6666600001 | Business consumer |
| EC | 5555500001 | End consumer |

---

## Quick Start Commands Summary

### Terminal 1 - Backend API
```bash
cd C:\Projects\DeliverX\src\DeliverX.API
dotnet run
```

### Terminal 2 - Frontend UI
```bash
cd C:\Projects\DeliverX\deliverx-ui
npm start
```

---

## Configuration Files

### API Configuration
File: `src/DeliverX.API/appsettings.json`

```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-chars",
    "AccessTokenExpirationMinutes": 10080,
    "RefreshTokenExpirationDays": 30
  },
  "OtpSettings": {
    "Length": 6,
    "ExpirationMinutes": 5,
    "MaxAttempts": 3,
    "RateLimitPerHour": 7
  }
}
```

### UI Configuration
File: `deliverx-ui/.env` (create if not exists)

```env
REACT_APP_API_URL=http://localhost:5205/api/v1
```

---

## Common Issues & Solutions

### Issue 1: "dotnet: command not found"
**Solution:** Install .NET SDK and restart terminal
```bash
# Verify installation
dotnet --version
```

### Issue 2: "npm: command not found"
**Solution:** Install Node.js and restart terminal
```bash
# Verify installation
node --version
npm --version
```

### Issue 3: API Build Fails
**Solution:** Restore packages
```bash
dotnet restore
dotnet build
```

### Issue 4: "Port 5205 already in use"
**Solution:** Kill existing process
```bash
# Windows
netstat -ano | findstr :5205
taskkill /PID <PID> /F

# Mac/Linux
lsof -i :5205
kill -9 <PID>
```

### Issue 5: "Port 3000 already in use"
**Solution:** Either kill the process or use different port
```bash
# Use different port
PORT=3001 npm start
```

### Issue 6: UI shows "Network Error"
**Solution:**
1. Make sure API is running (Terminal 1)
2. Check API URL: http://localhost:5205/swagger
3. Check browser console for CORS errors

### Issue 7: Database not found
**Solution:** Run migrations
```bash
cd src/DeliverX.API
dotnet ef database update
```

### Issue 8: "Entity Framework tools not found"
**Solution:** Install EF tools
```bash
dotnet tool install --global dotnet-ef
```

---

## Stopping the Application

### Stop API (Terminal 1)
Press `Ctrl + C`

### Stop UI (Terminal 2)
Press `Ctrl + C`

---

## Running in Production Mode

### Build UI for Production
```bash
cd deliverx-ui
npm run build
```
This creates optimized files in `build/` folder.

### Build API for Production
```bash
cd src/DeliverX.API
dotnet publish -c Release -o ./publish
```
Run with:
```bash
cd publish
dotnet DeliverX.API.dll
```

---

## Database Location

The SQLite database is located at:
```
src/DeliverX.API/deliverx.db
```

To reset database (delete all data):
```bash
# Delete the database file
rm src/DeliverX.API/deliverx.db

# Recreate with migrations
cd src/DeliverX.API
dotnet ef database update
```

---

## Development Tools (Optional)

### Recommended IDE
- **Backend:** Visual Studio 2022 or VS Code with C# extension
- **Frontend:** VS Code with ES7+ React extension

### Database Viewer
- **SQLite Browser:** https://sqlitebrowser.org/
- Open `deliverx.db` to view tables and data

### API Testing
- **Postman:** https://www.postman.com/
- **Swagger:** http://localhost:5205/swagger (built-in)

---

## Folder Permissions (Mac/Linux)

If you get permission errors:
```bash
chmod -R 755 DeliverX/
```

---

## Next Steps

After setup is complete:
1. Read `testing/01_Admin_Test.md` to test Admin features
2. Read `testing/06_Delivery_Flow.md` to understand delivery lifecycle
3. Create users for each role and test the flow

---

## Support

For issues:
1. Check this guide's troubleshooting section
2. Check GitHub Issues
3. Review API logs in terminal for errors
