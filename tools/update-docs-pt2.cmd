setlocal

git pull
git checkout gh-pages
if ERRORLEVEL 1 goto Error

copy /y docs\*.html .

git add *.html
if ERRORLEVEL 1 goto Error

git commit -m "UpdateDocs"
if ERRORLEVEL 1 goto Error

git push origin gh-pages
if ERRORLEVEL 1 goto Error

if ERRORLEVEL 1 goto Error

:Error
git checkout master
endlocal
exit /b %ERRORLEVEL%
