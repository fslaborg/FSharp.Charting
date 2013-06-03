
REM To use this script:
REM  - Make sure you have `fsi` in the PATH
REM  - Make sure you do not have any pending changes in `git status`
REM  - Go to the 'tools' directory and run `./update-docs.sh`

fsi build.fsx
copy /y update-docs-pt2.cmd ..\..\tmp.cmd
cd ..
..\tmp.cmd
