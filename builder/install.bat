@echo off

dotnet --version
if %errorlevel% neq 0 (
	echo "Dotnet is not installed, attempting to install it now..."
	curl https://download.visualstudio.microsoft.com/download/pr/a0832b5a-6900-442b-af79-6ffddddd6ba4/e2df0b25dd851ee0b38a86947dd0e42e/dotnet-runtime-5.0.17-win-x64.exe -o dotnet_install.exe
	echo "Dotnet installer downloaded, running"
	dotnet_install.exe /install /quiet /norestart /log dotnetlog.txt
	del dotnet_install.exe
	echo "Dotnet should now be installed"
)



"SML/SMLInstaller.exe"