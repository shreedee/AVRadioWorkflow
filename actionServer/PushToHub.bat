@echo off
setlocal EnableDelayedExpansion

rem We want this first thing else the Docker context will be unnecessarily bloated
echo clearing release folder
md .\release
del .\release\*.tar
del .\release\*.zip

set /P revversion=<versionNo.txt

if "!revversion!"=="" (
    set /P revversion=1
) else (
    set /A revversion=revversion+1
)

echo building labizbille/radioactions:1.0.%revversion%
echo %revversion% > versionNo.txt
docker build -t labizbille/radioactions:1.0.%revversion% -t labizbille/radioactions:currentBuild .

echo creating portbale image %CD%/release/newrevadmin_1.0.%revversion%.tar .........

docker save --output %CD%/release/newrevadmin_1.0.%revversion%.tar labizbille/radioactions:1.0.%revversion%

echo all done
pause 

