$PluginJson = Get-Content -Raw -Path '.\plugin.json' | ConvertFrom-Json
$Name = $PluginJson.Name # Weather
$Version = $PluginJson.Version # x.x.x
$NameVer = "$Name-$Version" # Weather-x.x.x
$ActionKeyword = $PluginJson.ActionKeyword
$RunTimeIdentifier = 'win-x64'
$PluginsDir = "$env:APPDATA\FlowLauncher\Plugins\"
$PublishDir = "$PluginsDir\$NameVer"

$FullName = "Flow.Launcher.Plugin.$Name"
$ProjectFile = ".\$FullName.csproj"

if (!$Name) {
    Write-Host 'Invalid Name'
    Exit
}

# Stop Flow Launcher if it's running
Write-Host "Stopping Flow Launcher"
Do {
    $Flow = Get-Process | Where-Object -Property ProcessName -eq 'Flow.Launcher'
    if ($Flow) {
        Stop-Process $Flow
        Start-Sleep 1
    }
} Until (!$Flow)

# Remove old plugin folders
Write-Host "Removing old plugin folders"
$Folders = Get-ChildItem -Path "$PluginsDir" | Where-Object { $_.Name -match "$Name-\d+\.\d+\.\d+" }
foreach ($Folder in $Folders) {
    Write-Host "Removing $Folder"
    Remove-Item -Recurse -Path "$PluginsDir\$($Folder.Name)" -Force -ErrorAction Stop
}

# create the folder $NameVer in $env:APPDATA\FlowLauncher\Plugins\
if (-not (Test-Path $PublishDir)) {
    Write-Host "Creating $PublishDir folder"
    New-Item -ItemType Directory -Path $PublishDir
}

# Build the plugin and output to $PublishDir
dotnet publish -c Release -r $RunTimeIdentifier --no-self-contained $ProjectFile -o $PublishDir

# Start Flow Launcher
$Flow = Start-Process $env:LOCALAPPDATA\FlowLauncher\Flow.Launcher.exe -PassThru

$Choices = @('&Yes', '&No')

# Prompt user to create a zip file
$ChoiceZip = $Host.UI.PromptForChoice('Create Zip', 'Do you want to create a zip file?', $Choices, 1)

# Create zip file (-CompressionLevel NoCompression) if user opted for it
if ($ChoiceZip -eq 0) {
    Write-Host "Creating Zip of .\bin\Release\$RunTimeIdentifier\ folder to .\$NameVer.zip"
    Compress-Archive -Path ".\bin\Release\$RunTimeIdentifier\" -DestinationPath ".\$NameVer.zip" -CompressionLevel NoCompression
}