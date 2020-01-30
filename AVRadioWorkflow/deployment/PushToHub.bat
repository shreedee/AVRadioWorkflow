@echo off
xcopy /Y /F Dockerfile ..\bin\publish
set /p revversion=<version.txt

echo building labizbille/avradioworkflow:%revversion%
docker build -t labizbille/avradioworkflow:%revversion% -t labizbille/avradioworkflow:currentBuild ../bin/publish

echo ready to publish labizbille/avradioworkflow:%revversion%, Press CTRL-C to exit or any key to continue
pause
docker push labizbille/avradioworkflow:%revversion%

echo all done
pause 