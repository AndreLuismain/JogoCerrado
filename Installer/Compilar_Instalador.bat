@echo off
chcp 65001 >nul
echo ============================================================================
echo   Compilador do Instalador EICerrado Beta (Inno Setup 6)
echo ============================================================================

if not exist "..\Builds\Beta_EICerrado\EICerrado.exe" (
    echo [AVISO] O executavel "..\Builds\Beta_EICerrado\EICerrado.exe" ainda nao foi gerado.
    echo Abra o projeto no Unity Editor e clique no menu superior:
    echo   Cerrado -^> 7. Preparar Tudo e Gerar Build Beta (Windows x64)
    echo (Esse comando ja gera a Build e compila este instalador automaticamente!)
    pause
    exit /b 1
)

set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=C:\Program Files\Inno Setup 6\ISCC.exe"

if not exist "%ISCC%" (
    echo [ERRO] Inno Setup 6 (ISCC.exe) nao encontrado.
    pause
    exit /b 1
)

echo Compilando "EICerrado_Beta_Setup.iss"...
"%ISCC%" "%~dp0EICerrado_Beta_Setup.iss"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [SUCESSO] Instalador gerado em: "..\Builds\Installer\EICerrado_Beta_Setup.exe"
) else (
    echo.
    echo [ERRO] Falha ao compilar o instalador.
)
pause
