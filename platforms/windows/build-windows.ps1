param(
    [ValidateSet("All", "x64", "ARM64")]
    [string]$Architecture = "All",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$Pack
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$project = Join-Path $PSScriptRoot "src/RaTeX.Windows/RaTeX.Windows.csproj"
$testProject = Join-Path $PSScriptRoot "tests/RaTeX.Windows.Tests/RaTeX.Windows.Tests.csproj"
$architectures = if ($Architecture -eq "All") { @("x64", "ARM64") } else { @($Architecture) }

if ($Pack -and $Architecture -ne "All") {
    throw "Package creation requires -Architecture All so both Windows runtimes are included."
}

Push-Location $root
try {
    foreach ($current in $architectures) {
        $target = if ($current -eq "ARM64") { "aarch64-pc-windows-msvc" } else { "x86_64-pc-windows-msvc" }
        $runtime = if ($current -eq "ARM64") { "win-arm64" } else { "win-x64" }
        $destination = Join-Path $PSScriptRoot "src/RaTeX.Windows/runtimes/$runtime/native"

        rustup target add $target
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to install Rust target $target."
        }

        $cargoArguments = @("build", "--package", "ratex-ffi", "--target", $target)
        if ($Configuration -eq "Release") {
            $cargoArguments += @("--profile", "interview-pilot-windows")
        }

        cargo @cargoArguments
        if ($LASTEXITCODE -ne 0) {
            throw "RaTeX native build failed for $target."
        }

        New-Item -ItemType Directory -Force -Path $destination | Out-Null
        $cargoProfile = if ($Configuration -eq "Release") { "interview-pilot-windows" } else { "debug" }
        Copy-Item `
            -LiteralPath (Join-Path $root "target/$target/$cargoProfile/ratex_ffi.dll") `
            -Destination (Join-Path $destination "ratex_ffi.dll") `
            -Force

        dotnet build $project `
            --configuration $Configuration `
            --runtime $runtime `
            -p:PlatformTarget=$current
        if ($LASTEXITCODE -ne 0) {
            throw "RaTeX Windows managed build failed for $current."
        }
    }

    $hostArchitecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
    $canRunArm64 = $hostArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64 -and `
        $architectures -contains "ARM64"
    $testArchitecture = if ($canRunArm64) {
        "ARM64"
    } elseif ($architectures -contains "x64") {
        "x64"
    } else {
        $null
    }

    if ($testArchitecture) {
        $testRuntime = if ($testArchitecture -eq "ARM64") { "win-arm64" } else { "win-x64" }
        dotnet test $testProject `
            --configuration $Configuration `
            --runtime $testRuntime `
            -p:PlatformTarget=$testArchitecture
        if ($LASTEXITCODE -ne 0) {
            throw "RaTeX Windows tests failed for $testArchitecture."
        }
    } else {
        Write-Host "Managed tests were not executed because the selected native architecture cannot run on this host."
    }

    if ($Pack) {
        dotnet pack $project --configuration $Configuration
        if ($LASTEXITCODE -ne 0) {
            throw "RaTeX Windows package creation failed."
        }
    }
}
finally {
    Pop-Location
}
