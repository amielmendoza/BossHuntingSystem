@if "%SCM_TRACE_LEVEL%" NEQ "4" @echo off

:: ----------------------
:: KUDU Deployment Script
:: Version: 1.0.17
:: ----------------------

:: Prerequisites
:: -------------

:: Verify node.js installed
where node 2>nul >nul
IF %ERRORLEVEL% NEQ 0 (
  echo Missing node.js executable, please install node.js, if already installed make sure it can be reached from current environment.
  goto error
)

:: Setup
:: -----

setlocal enabledelayedexpansion

SET ARTIFACTS=%~dp0%..\artifacts

IF NOT DEFINED DEPLOYMENT_SOURCE (
  SET DEPLOYMENT_SOURCE=%~dp0%.
)

IF NOT DEFINED DEPLOYMENT_TARGET (
  SET DEPLOYMENT_TARGET=%ARTIFACTS%\wwwroot
)

IF NOT DEFINED NEXT_MANIFEST_PATH (
  SET NEXT_MANIFEST_PATH=%ARTIFACTS%\manifest

  IF NOT DEFINED PREVIOUS_MANIFEST_PATH (
    SET PREVIOUS_MANIFEST_PATH=%ARTIFACTS%\manifest
  )
)

IF NOT DEFINED KUDU_SYNC_CMD (
  :: Install kudu sync
  echo Installing Kudu Sync
  call npm install kudusync -g --silent
  IF !ERRORLEVEL! NEQ 0 goto error

  :: Locally just running "kuduSync" would also work
  SET KUDU_SYNC_CMD=%appdata%\npm\kuduSync.cmd
)

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: Deployment
:: ----------

echo Handling ASP.NET Core Web Application deployment.

:: 1. Restore NuGet packages
IF EXIST "%DEPLOYMENT_SOURCE%\BossHuntingSystem.Server\BossHuntingSystem.Server.csproj" (
  call :ExecuteCmd dotnet restore "%DEPLOYMENT_SOURCE%\BossHuntingSystem.Server\BossHuntingSystem.Server.csproj"
  IF !ERRORLEVEL! NEQ 0 goto error
)

:: 2. Build and publish
IF EXIST "%DEPLOYMENT_SOURCE%\BossHuntingSystem.Server\BossHuntingSystem.Server.csproj" (
  call :ExecuteCmd dotnet publish "%DEPLOYMENT_SOURCE%\BossHuntingSystem.Server\BossHuntingSystem.Server.csproj" --output "%DEPLOYMENT_TARGET%" --configuration Release
  IF !ERRORLEVEL! NEQ 0 goto error
)

:: 3. Install NPM packages for Angular client
IF EXIST "%DEPLOYMENT_SOURCE%\bosshuntingsystem.client\package.json" (
  pushd "%DEPLOYMENT_SOURCE%\bosshuntingsystem.client"
  call :ExecuteCmd npm install
  IF !ERRORLEVEL! NEQ 0 goto error
  popd
)

:: 4. Build Angular application for production
IF EXIST "%DEPLOYMENT_SOURCE%\bosshuntingsystem.client\package.json" (
  pushd "%DEPLOYMENT_SOURCE%\bosshuntingsystem.client"
  echo Building Angular application for production...
  call :ExecuteCmd npm run build:prod
  IF !ERRORLEVEL! NEQ 0 goto error
  echo Angular production build completed successfully.
  popd
)

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
goto end

:: Execute command routine that will echo out when error
:ExecuteCmd
setlocal
set _CMD_=%*
call %_CMD_%
if "%ERRORLEVEL%" NEQ "0" echo Failed exitCode=%ERRORLEVEL%, command=%_CMD_%
exit /b %ERRORLEVEL%

:error
endlocal
echo An error has occurred during web site deployment.
call :exitSetErrorLevel
call :exitFromFunction 2>nul

:exitSetErrorLevel
exit /b 1

:exitFromFunction
()

:end
endlocal
echo Finished successfully.
