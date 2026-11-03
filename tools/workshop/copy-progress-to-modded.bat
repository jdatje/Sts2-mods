@echo off
rem Slay the Spire 2: copy your normal (unmodded) progress into the modded save,
rem so modded play starts with all your unlocks. Close the game before running this.
rem A full backup is made on your Desktop first.

setlocal enabledelayedexpansion
set "ROOT=%APPDATA%\SlayTheSpire2\steam"
if not exist "%ROOT%" (
  echo Could not find "%ROOT%". Start Slay the Spire 2 once, close it, and try again.
  pause & exit /b 1
)

tasklist /fi "imagename eq SlayTheSpire2.exe" | find /i "SlayTheSpire2.exe" >nul
if not errorlevel 1 (
  echo Slay the Spire 2 is still running. Close the game first, then run this again.
  pause & exit /b 1
)

for /f "tokens=1-3 delims=/-. " %%a in ("%date%") do set "STAMP=%%c%%b%%a"
set "BACKUP=%USERPROFILE%\Desktop\SlayTheSpire2-save-backup-%STAMP%-%RANDOM%"
echo Making a backup in "%BACKUP%" ...
robocopy "%APPDATA%\SlayTheSpire2" "%BACKUP%" /E /NFL /NDL /NJH /NJS >nul

for /d %%U in ("%ROOT%\*") do (
  rem account-wide progress (character unlocks etc.) lives in the loose .save files next to the profiles
  if not exist "%%U\modded" mkdir "%%U\modded"
  for %%F in ("%%U\*.save") do (
    echo Copying %%~nxF -^> modded\%%~nxF  ^(%%~nxU^)
    copy /y "%%F" "%%U\modded\" >nul
  )
  for %%P in (profile1 profile2 profile3) do (
    if exist "%%U\%%P" (
      echo Copying %%P -^> modded\%%P  ^(%%~nxU^)
      robocopy "%%U\%%P" "%%U\modded\%%P" /E /NFL /NDL /NJH /NJS >nul
    )
  )
)

echo.
echo Done. Start the game with mods: your characters and unlocks should be there.
echo Still locked? Steam Cloud may have put back an older modded save: turn off "Keep game saves
echo in Steam Cloud" for Slay the Spire 2, run this again, start the game, then turn it back on.
pause
