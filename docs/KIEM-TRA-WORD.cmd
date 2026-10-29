@echo off
setlocal
pushd "%~dp0"
if not exist "ChinhTa.Desktop.exe" (
  echo Missing ChinhTa.Desktop.exe. Extract the entire preview ZIP first.
  popd
  pause
  exit /b 2
)
echo Checking synthetic samples in separate Word sessions.
echo Please wait. Existing user documents are not selected by this command.
start "" /wait "%~dp0ChinhTa.Desktop.exe" --word-check isolated-spacing,spacing-calibration,technical-text
set "ctv_word_check_exit=%errorlevel%"
echo.
echo Exit code: %ctv_word_check_exit%  ^(0=all selected PASS, 1=FAIL/NOT_RUN, 2=invalid command^)
echo Reports: "%LOCALAPPDATA%\ChinhTaViet\OfficeGroups-*\office-groups.txt"
echo This result applies only to the three selected sample groups.
popd
pause
exit /b %ctv_word_check_exit%
