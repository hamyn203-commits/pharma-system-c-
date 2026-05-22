@echo off
chcp 65001 >nul
echo ========================================================
echo جاري تشغيل نظام مخزن الندا (API + Admin App)
echo ========================================================

echo [1/2] تشغيل الخادم (API Server)...
start "AlNeda API" cmd /c "title AlNeda API && cd /d "%~dp0" && dotnet run --project src\AlNeda.API\AlNeda.API.csproj --urls http://localhost:5000"

echo يرجى الانتظار بضع ثواني ليتم تشغيل الخادم...
timeout /t 5 /nobreak >nul

echo.
echo [2/2] تشغيل برنامج الإدارة (Admin App)...
start "AlNeda Admin" cmd /c "title AlNeda Admin && cd /d "%~dp0" && dotnet run --project src\AlNeda.Admin\AlNeda.Admin.csproj"

echo.
echo تم الانتهاء! يمكنك إغلاق هذه النافذة الآن (ولكن اترك نوافذ السيرفر والبرنامج مفتوحة).
timeout /t 3 >nul
exit
