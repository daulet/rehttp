Write-Host "Running unit test" -ForegroundColor Blue

dotnet test .\test\rehttp.UnitTests\rehttp.UnitTests.csproj --no-build

if ($LastExitCode -ne 0) {
    return $LastExitCode
}

Write-Host "Running integration test" -ForegroundColor Blue

function Start-AzureFunction ([int]$port, [string]$workingDir) {
    Write-Host "Copying to tmp/$workingDir"
    Copy-Item -Path $workingDir -Destination "tmp/$workingDir" -Recurse
    Write-Host "Starting function at port $port from tmp/$workingDir"
    Start-Process func -ArgumentList "host start --port $port" -WorkingDirectory "tmp/$workingDir" -PassThru
}

Start-Process redis-server -PassThru
Start-AzureFunction 7072 -workingDir "src\rehttp\bin\Debug\netstandard2.0"
Start-AzureFunction 7073 -workingDir "test\rehttp.Mocks\bin\Debug\netstandard2.0"

dotnet test .\test\rehttp.IntegrationTests\rehttp.IntegrationTests.csproj --no-build

if ($LastExitCode -ne 0) {
    return $LastExitCode
}

Stop-Process (Get-Process func).Id
