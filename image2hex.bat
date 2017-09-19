@echo off
cd /d %~dp0
make
if not %ERRORLEVEL% == 0 (
	exit /b %ERRORLEVEL%
)
exit /b 0