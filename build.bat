@echo off
:: Build launcher_gui.exe using PyInstaller
:: Requirements: pip install pyinstaller
:: Output: dist/launcher_gui.exe (~10-15MB)

echo [Build] Installing PyInstaller if needed...
pip install pyinstaller >nul 2>&1

echo [Build] Building launcher_gui.exe...
pyinstaller --onefile --windowed --noconfirm ^
    --name "D2R_Launcher" ^
    --add-data "gui;gui" ^
    --exclude-module matplotlib ^
    --exclude-module numpy ^
    --exclude-module pandas ^
    --exclude-module scipy ^
    --exclude-module PIL ^
    --exclude-module cv2 ^
    --exclude-module pytest ^
    launcher_gui.py

echo.
if exist "dist\D2R_Launcher.exe" (
    echo [Build] Success! Output: dist\D2R_Launcher.exe
    echo [Build] Copy D2R_Launcher.exe to the same directory as multi_play_d2r.bat to use.
) else (
    echo [Build] Failed! Check the output above for errors.
)
pause
