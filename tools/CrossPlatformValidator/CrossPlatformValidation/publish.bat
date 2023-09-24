 dotnet publish -r win-x64 -c release -f net8.0
 copy  .\CrossPlatformValidation\bin\Release\net8.0\win-x64\native\* CrossPlatformValidatorTests\bin\Debug\net8.0\

 dotnet publish -r win-x64 -c release -f net7.0
 copy  .\CrossPlatformValidation\bin\Release\net7.0\win-x64\native\* CrossPlatformValidatorTests\bin\Debug\net7.0\
