@echo off
cls
echo Running git status...
git status

:: Ask user if they want to commit
set /p choice=Do you want to commit the changes? (y/n): 

if /I "%choice%"=="y" (
    set /p commit_msg=Enter commit message: 
    git add .
    git commit -m "%commit_msg%"
    git push origin master
    echo Commit and push completed!
) else (
    echo No commit was made.
)

pause
