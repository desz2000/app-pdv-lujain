@echo off
REM Sobe a API e o front do caixa do PDV Lujain em duas janelas separadas.
REM Duplo-clique pra iniciar; Ctrl+C em cada janela pra parar.

setlocal
set ROOT=%~dp0
set API_URL=http://localhost:5170
set CAIXA_URL=http://localhost:5180

echo.
echo === PDV Lujain ===
echo API   : %API_URL%
echo Caixa : %CAIXA_URL%
echo.
echo Subindo a API em uma janela e o caixa em outra...
echo Cada janela mostra os logs do seu servico. Feche-a (ou Ctrl+C) pra desligar.
echo.

start "PDV API" cmd /k "cd /d ""%ROOT%src\RestaurantePDV.API"" && dotnet run --urls %API_URL%"

REM Pequena pausa pra deixar a API subir antes do caixa tentar bater nela.
timeout /t 6 /nobreak >nul

start "PDV Caixa" cmd /k "cd /d ""%ROOT%src\RestaurantePDV.Desktop"" && dotnet run --urls %CAIXA_URL%"

REM Da mais um respiro pro caixa antes de abrir o navegador.
timeout /t 8 /nobreak >nul
start "" "%CAIXA_URL%"

echo.
echo Pronto. Caixa abrindo em %CAIXA_URL% no navegador.
echo Pra desligar, feche as janelas "PDV API" e "PDV Caixa".
echo.
endlocal
