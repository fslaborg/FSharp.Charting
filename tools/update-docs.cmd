
REM To use this script:
REM  - Make sure you have `fsi` in the PATH
REM  - Make sure you do not have any pending changes in `git status`
REM  - Go to the 'tools' directory and run `./update-docs.sh`

fsi build.fsx
if not exist ..\..\gh-pages\FSharp.Charting\index.html (
  mkdir ..\..\gh-pages
  pushd ..\..\gh-pages
  git clone https://github.com/fsharp/FSharp.Charting 
  pushd FSharp.Charting
  git checkout gh-pages
  popd
  popd
)

pushd ..\..\gh-pages\FSharp.Charting
git pull
popd

copy /y ..\docs\*.html ..\..\gh-pages\FSharp.Charting\

pushd ..\..\gh-pages\FSharp.Charting
git add *.html
git commit -m "UpdateDocs"
git push origin gh-pages
popd
