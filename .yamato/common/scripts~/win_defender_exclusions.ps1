Add-MpPreference -ExclusionPath 'C:\'
Add-MpPreference -ExclusionPath "C:\buildTemp"
Add-MpPreference -ExclusionPath "C:\CrashDump"
Add-MpPreference -ExclusionPath "C:\LogAgents"
Add-MpPreference -ExclusionPath "C:\localsymbols"
Add-MpPreference -ExclusionPath "%USERPROFILE%\AppData\Local\largefiles"
Add-MpPreference -ExclusionPath 'C:\Windows\Microsoft.NET'
Add-MpPreference -ExclusionPath 'C:\Windows\assembly'
Add-MpPreference -ExclusionPath 'C:\Program Files (x86)\MSBuild'
Add-MpPreference -ExclusionPath 'C:\Program Files (x86)\Microsoft SDKs'
Add-MpPreference -ExclusionPath 'C:\Program Files (x86)\Microsoft Visual Studio'
Add-MpPreference -ExclusionPath 'C:\Program Files (x86)\Microsoft Visual Studio ??.?'


Add-MpPreference -ExclusionProcess 'devenv.exe'
Add-MpPreference -ExclusionProcess 'dotnet.exe'
Add-MpPreference -ExclusionProcess 'msbuild.exe'
Add-MpPreference -ExclusionProcess 'node.exe'
Add-MpPreference -ExclusionProcess 'node.js'
Add-MpPreference -ExclusionProcess 'CodeCoverage.exe'
Add-MpPreference -ExclusionProcess 'IntelliTrace.exe'
Add-MpPreference -ExclusionProcess 'datacollector.exe'
Add-MpPreference -ExclusionProcess 'testhost.exe'
Add-MpPreference -ExclusionProcess 'bee.exe'
Add-MpPreference -ExclusionProcess 'jam.exe'
Add-MpPreference -ExclusionProcess 'cl.exe'
Add-MpPreference -ExclusionProcess 'link.exe'
Add-MpPreference -ExclusionProcess 'mono.exe'
Add-MpPreference -ExclusionProcess 'perl.exe'
Add-MpPreference -ExclusionProcess 'tundra.exe'
Add-MpPreference -ExclusionProcess 'unity.exe'
Add-MpPreference -ExclusionProcess 'windbg.exe'
Add-MpPreference -ExclusionProcess 'git.exe'
Add-MpPreference -ExclusionProcess 'hg.exe'
Add-MpPreference -ExclusionProcess 'csc.exe'
Add-MpPreference -ExclusionProcess 'il2cpp.exe'

Add-MpPreference -ExclusionProcess 'UnifiedTestRunner.exe'
Add-MpPreference -ExclusionProcess 'UnityShaderCompiler.exe'
Add-MpPreference -ExclusionProcess 'UnityPackageManager.exe'
Add-MpPreference -ExclusionProcess 'UnityCrashHandler64.exe'

Set-MpPreference -DisableCatchupFullScan $true -DisableArchiveScanning $true -DisableAutoExclusions $true -DisableBehaviorMonitoring $true -DisableBlockAtFirstSeen $true -DisableIOAVProtection $true -DisablePrivacyMode $true -DisableScanningNetworkFiles $true -DisableScriptScanning $true -DisableRealtimeMonitoring $true