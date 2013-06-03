
REM To use this script:
REM  - Make sure you have `fsi` in the PATH
REM  - Make sure you do not have any pending changes in `git status`
REM  - Go to the 'tools' directory and run `./update-docs.sh`

fsi build.fsx
cd ..
git pull
git checkout gh-pages
copy /y docs\*.html .
git add *.html
git commit -m "Update generated documentation"
git push origin gh-pages
git checkout master
