if [[ -d "app/publish/" ]]
then
    rm -rf "app/publish"
fi

dotnet publish -c Debug -o "app/publish" -f net9.0
