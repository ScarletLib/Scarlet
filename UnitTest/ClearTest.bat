
@echo off
echo Clearing Test Processes...
taskkill /f /im vstest.* 2> nul
exit 0 
if exist "$(TargetPath).locked" del "$(TargetPath).locked"  
if exist "$(TargetPath)" if not exist "$(TargetPath).locked" move "$(TargetPath)" "$(TargetPath).locked"