#Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process -Force
$doc = [xml](Get-Content -Path "../Directory.Build.Props")
$doc.Project.PropertyGroup[1].TargetFrameworks = "net6.0"
$path = [System.IO.Path]::GetFullPath($pwd.Path+"\..\Directory.Build.Props")
$doc.Save($path)
