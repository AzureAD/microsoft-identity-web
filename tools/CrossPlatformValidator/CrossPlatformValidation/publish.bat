dotnet publish CrossPlatformValidation -r win-x64 -c release -f net8.0
copy  .\CrossPlatformValidation\bin\Release\net8.0\win-x64\native\* CrossPlatformValidatorTests\bin\Debug\net8.0\
copy  .\CrossPlatformValidation\bin\Release\net8.0\win-x64\native\* Benchmark\bin\Release\net8.0\
