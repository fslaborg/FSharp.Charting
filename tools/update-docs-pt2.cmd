git pull
git checkout gh-pages
copy /y docs\*.html .
git add *.html
git commit -m "UpdateDocs"
git push origin gh-pages

:Error
git checkout master
