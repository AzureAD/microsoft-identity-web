if exist "app/publish/" (
    rmdir /s /q "app/publish"
)

dotnet publish -c Debug -o "./app/publish" -f net9.0
