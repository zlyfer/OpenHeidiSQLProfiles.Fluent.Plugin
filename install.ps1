Stop-Process -Name "fluentsearch" -ErrorAction SilentlyContinue

dotnet restore
dotnet publish -c Release -r win10-x64

$pluginDir = "$env:LOCALAPPDATA\Packages\21814BlastApps.BlastSearch_pdn8zwjh47aq4\LocalCache\Roaming\Blast\FluentSearchPlugins\Open HeidiSQL Profiles"
mkdir $pluginDir -Force

Copy-Item "OpenHeidiSQLProfiles.Fluent.Plugin\bin\Release\net7.0\win10-x64\publish\*.dll" -Destination $pluginDir -Force
Copy-Item "OpenHeidiSQLProfiles.Fluent.Plugin\pluginsInfo.json" -Destination $pluginDir -Force

Start-Process "C:\Program Files\WindowsApps\21814BlastApps.BlastSearch_1.0.1.0_x64__pdn8zwjh47aq4\FluentSearch.exe" -ErrorAction SilentlyContinue