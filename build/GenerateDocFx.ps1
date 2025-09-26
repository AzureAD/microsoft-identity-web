#Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process -Force
Copy-Item -Path "../Directory.Build.Props" -Destination "./Directory.Build.Props.save"
$doc = [xml](Get-Content -Path "../Directory.Build.Props")
$doc.Project.PropertyGroup[1].TargetFrameworks = "net9.0"
$path = [System.IO.Path]::GetFullPath($pwd.Path+"\..\Directory.Build.Props")
$doc.Save($path)
docfx "../docfx_project/docfx.json" --serve
Copy-Item -Path "./Directory.Build.Props.save" -Destination "../Directory.Build.Props"
