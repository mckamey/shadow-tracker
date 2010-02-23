@ECHO OFF
:: -------------------------------------------------------------------

ECHO WARNING: are you sure to uninstall ShadowTracker.Service.exe as a Windows Service?
PAUSE
ECHO.

ShadowTracker.Service.exe /uninstall
PAUSE
