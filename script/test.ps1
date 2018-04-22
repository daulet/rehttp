Write-Host "Running unit test" -ForegroundColor Blue

dotnet test .\test\rehttp.UnitTests\rehttp.UnitTests.csproj

if ($LastExitCode -ne 0) {
    return $LastExitCode
}
