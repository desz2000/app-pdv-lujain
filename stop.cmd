@echo off
REM Encerra a API e o caixa do PDV Lujain (mata as janelas abertas pelo start.cmd).

echo Encerrando "PDV API"...
taskkill /FI "WINDOWTITLE eq PDV API*" /T /F >nul 2>&1

echo Encerrando "PDV Caixa"...
taskkill /FI "WINDOWTITLE eq PDV Caixa*" /T /F >nul 2>&1

echo Pronto.
