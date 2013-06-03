
REM To use this script:
REM  - Make sure you have `fsi` in the PATH
REM  - Make sure you do not have any pending changes in `git status`
REM  - Go to the 'tools' directory and run `./update-docs.sh`

fsi build.fsx
git checkout gh-pages
cp ../docs/experimental/*.html ../experimental/
cp ../docs/library/*.html ../library/
cp ../docs/tutorials/*.html ../tutorials/
cp ../docs/*.html ../
git commit -a -m "Update generated documentation"
git push
git checkout master