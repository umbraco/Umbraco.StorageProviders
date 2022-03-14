
  param (
    # get, don't execute
    [Parameter(Mandatory=$false)]
    [Alias("g")]
    [switch] $get = $false,

    # run local, don't download, assume everything is ready
    [Parameter(Mandatory=$false)]
    [Alias("l")]
    [Alias("loc")]
    [switch] $local = $false,

    # keep the build directories, don't clear them
    [Parameter(Mandatory=$false)]
    [Alias("c")]
    [Alias("cont")]
    [switch] $continue = $false
  )

  # ################################################################
  # BOOTSTRAP
  # ################################################################

  # create and boot the buildsystem
  $ubuild = &"$PSScriptRoot\build-bootstrap.ps1"
  if (-not $?) { return }
  $ubuild.Boot($PSScriptRoot,
    @{ Local = $local; },
    @{ Continue = $continue })
  if ($ubuild.OnError()) { return }

  Write-Host "Umbraco StorageProviders Build"
  Write-Host "Umbraco.Build v$($ubuild.BuildVersion)"

  # ################################################################
  # TASKS
  # ################################################################

  $ubuild.DefineMethod("CompileUmbracoStorageProviders",
  {
    $buildConfiguration = "Release"

    $src = "$($this.SolutionRoot)\src"
    $log = "$($this.BuildTemp)\dotnet.build.umbraco.log"

    Write-Host "Compile Umbraco.StorageProviders.AzureBlob"
    Write-Host "Logging to $log"

    &dotnet build "$src\Umbraco.StorageProviders.AzureBlob\Umbraco.StorageProviders.AzureBlob.csproj" `
      --configuration $buildConfiguration `
      --output "$($this.BuildTemp)\bin\\" `
      > $log

    # get files into WebApp\bin
    &dotnet publish "$src\Umbraco.StorageProviders.AzureBlob\Umbraco.StorageProviders.AzureBlob.csproj" `
      --configuration Release --output "$($this.BuildTemp)\WebApp\bin\\" `
      > $log

    # remove extra files
    $webAppBin = "$($this.BuildTemp)\WebApp\bin"
    $excludeDirs = @("$($webAppBin)\refs","$($webAppBin)\runtimes","$($webAppBin)\Umbraco","$($webAppBin)\wwwroot")
    $excludeFiles = @("$($webAppBin)\appsettings.*","$($webAppBin)\*.deps.json","$($webAppBin)\*.exe","$($webAppBin)\*.config","$($webAppBin)\*.runtimeconfig.json")
    $this.RemoveDirectory($excludeDirs)
    $this.RemoveFile($excludeFiles)

    if (-not $?) { throw "Failed to compile Umbraco.StorageProviders." }

    # /p:UmbracoBuild tells the csproj that we are building from PS, not VS
  })


  $ubuild.DefineMethod("PreparePackages",
  {
    Write-Host "Prepare Packages"

    $src = "$($this.SolutionRoot)\src"
    $tmp = "$($this.BuildTemp)"
    $out = "$($this.BuildOutput)"

    $buildConfiguration = "Release"

    # cleanup build
    Write-Host "Clean build"
    $this.RemoveFile("$tmp\bin\*.dll.config")

    # cleanup WebApp
    Write-Host "Cleanup WebApp"
    $this.RemoveDirectory("$tmp\WebApp")

    # offset the modified timestamps on all umbraco dlls, as WebResources
    # break if date is in the future, which, due to timezone offsets can happen.
    Write-Host "Offset dlls timestamps"
    Get-ChildItem -r "$tmp\*.dll" | ForEach-Object {
      $_.CreationTime = $_.CreationTime.AddHours(-11)
      $_.LastWriteTime = $_.LastWriteTime.AddHours(-11)
    }
  })

  $ubuild.DefineMethod("PrepareBuild",
  {
    Write-Host "============ PrepareBuild ============"

    Write-host "Set environment"
    $env:UMBRACO_VERSION=$this.Version.Semver.ToString()
    $env:UMBRACO_RELEASE=$this.Version.Release
    $env:UMBRACO_COMMENT=$this.Version.Comment
    $env:UMBRACO_BUILD=$this.Version.Build

    if ($args -and $args[0] -eq "vso")
    {
      Write-host "Set VSO environment"
      # set environment variable for VSO
      # https://github.com/Microsoft/vsts-tasks/issues/375
      # https://github.com/Microsoft/vsts-tasks/blob/master/docs/authoring/commands.md
      Write-Host ("##vso[task.setvariable variable=UMBRACO_VERSION;]$($this.Version.Semver.ToString())")
      Write-Host ("##vso[task.setvariable variable=UMBRACO_RELEASE;]$($this.Version.Release)")
      Write-Host ("##vso[task.setvariable variable=UMBRACO_COMMENT;]$($this.Version.Comment)")
      Write-Host ("##vso[task.setvariable variable=UMBRACO_BUILD;]$($this.Version.Build)")

      Write-Host ("##vso[task.setvariable variable=UMBRACO_TMP;]$($this.SolutionRoot)\build.tmp")
    }
  })

  $ubuild.DefineMethod("RestoreNuGet",
  {
    Write-Host "Restore NuGet"
    Write-Host "Logging to $($this.BuildTemp)\nuget.restore.log"
    &$this.BuildEnv.NuGet restore "$($this.SolutionRoot)\Umbraco.StorageProviders.sln" > "$($this.BuildTemp)\nuget.restore.log"
    if (-not $?) { throw "Failed to restore NuGet packages." }
  })

  $ubuild.DefineMethod("PackageNuGet",
  {
    Write-Host "Create NuGet packages"

    $log = "$($this.BuildTemp)\dotnet.pack.umbraco.log"
    Write-Host "Logging to $log"

    &dotnet pack "Umbraco.StorageProviders.sln" `
        --output "$($this.BuildOutput)" `
        --verbosity detailed `
        -c Release `
        -p:PackageVersion="$($this.Version.Semver.ToString())" > $log

    # run hook
    if ($this.HasMethod("PostPackageNuGet"))
    {
      Write-Host "Run PostPackageNuGet hook"
      $this.PostPackageNuGet();
      if (-not $?) { throw "Failed to run hook." }
    }
  })

  $ubuild.DefineMethod("Build",
  {
    $error.Clear()

  #  $this.PrepareBuild()
  #  if ($this.OnError()) { return }
    $this.RestoreNuGet()
    if ($this.OnError()) { return }
    $this.CompileUmbracoStorageProviders()
    if ($this.OnError()) { return }
    $this.PreparePackages()
    if ($this.OnError()) { return }
    $this.PackageNuGet()
    if ($this.OnError()) { return }
    Write-Host "Done"
  })

  # ################################################################
  # RUN
  # ################################################################

  # configure
  $ubuild.ReleaseBranches = @( "master" )

  # run
  if (-not $get)
  {
    $ubuild.Build()
    if ($ubuild.OnError()) { return }
  }
  if ($get) { return $ubuild }
