 dotnet publish -r win-x64 -c release -f net8.0
 copy bin\release\net8.0\win-x64\native\* ..\CrossPlatformValidatorTests\bin\debug\net8.0
