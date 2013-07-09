// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#r @"tools\FAKE\tools\FakeLib.dll"

open System
open System.IO
open Fake 
open Fake.AssemblyInfoFile

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

let files includes = 
  { BaseDirectories = [__SOURCE_DIRECTORY__]
    Includes = includes
    Excludes = [] } |> Scan

// Information about the project to be used at NuGet and in AssemblyInfo files
let project = "FSharp.Charting"
let authors = ["Carl Nolan, Don Syme, Tomas Petricek"]
let summary = "A Charting Library for F#"
let description = """
  The FSharp.Charting library is a charting library for F# data scripting."""
let tags = "F# FSharpChart charting plotting"


// Information for the project containing experimental providers
let projectAspNet = "FSharp.Charting.AspNet"
let summaryAspNet = summary + " (ASP.NET web forms)"
let tagsAspNet = tags + " ASPNET"
let descriptionAspNet = """
  The FSharp.Charting.AspNet library is a charting library for ASP.NET Web Forms charts using the same chart specifications as FSharp.Charting."""


// Read additional information from the release notes document
let releaseNotes, version = 
    let lastItem = File.ReadLines "RELEASE_NOTES.md" |> Seq.last
    let firstDash = lastItem.IndexOf('-')
    ( lastItem.Substring(firstDash + 1 ).Trim(), 
      lastItem.Substring(0, firstDash).Trim([|'*'|]).Trim() )

// --------------------------------------------------------------------------------------
// Generate assembly info files with the right version & up-to-date information

Target "AssemblyInfo" (fun _ ->
    [ ("src/AssemblyInfo.fs", "FSharp.Charting", project, summary)
      ( "src/AssemblyInfo.AspNet.fs", "FSharp.Charting.AspNet", projectAspNet, summaryAspNet )
    ]
    |> Seq.iter (fun (fileName, title, project, summary) ->
        CreateFSharpAssemblyInfo fileName
           [ Attribute.Title title
             Attribute.Product project
             Attribute.Description summary
             Attribute.Version version
             Attribute.FileVersion version] )
)

// --------------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ ->
    CleanDirs ["bin"]
)

// --------------------------------------------------------------------------------------
// Build library (builds Visual Studio solution, which builds multiple versions
// of the runtime library & desktop + Silverlight version of design time library)

Target "Build" (fun _ ->
    (files ["FSharp.Charting.sln"; "FSharp.Charting.Tests.sln"])
    |> MSBuildRelease "" "Rebuild"
    |> ignore
)
// --------------------------------------------------------------------------------------
// Run the unit tests using test runner & kill test runner when complete

Target "RunTests" (fun _ ->

    // Will get NUnit.Runner NuGet package if not present
    // (needed to run tests using the 'NUnit' target)
    RestorePackages()

    let nunitVersion = GetPackageVersion "packages" "NUnit.Runners"
    let nunitPath = sprintf "packages/NUnit.Runners.%s/Tools" nunitVersion

    ActivateFinalTarget "CloseTestRunner"

    (files ["tests/bin/Release/FSharp.Charting.Tests.dll"])
    |> NUnit (fun p ->
        { p with
            ToolPath = nunitPath
            DisableShadowCopy = true
            TimeOut = TimeSpan.FromMinutes 20.
            OutputFile = "TestResults.xml" })
)

FinalTarget "CloseTestRunner" (fun _ ->  
    ProcessHelper.killProcess "nunit-agent.exe"
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->

    // Format the description to fit on a single line (remove \r\n and double-spaces)
    let description = description.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
    //let descriptionExperimental = descriptionExperimental.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
    let nugetPath = "tools/Nuget/nuget.exe"

    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = version
            ReleaseNotes = releaseNotes
            Tags = tagsAspNet
            OutputPath = "bin"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] })
        "nuget/FSharp.Charting.nuspec"

    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = projectAspNet
            Summary = summaryAspNet
            Description = descriptionAspNet
            Version = version
            ReleaseNotes = releaseNotes
            Tags = tags
            OutputPath = "bin"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] })
        "nuget/FSharp.Charting.AspNet.nuspec"

)

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build target=<Target>' to verride

Target "All" DoNothing

"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "RunTests"
  ==> "NuGet"
  ==> "All"

Run <| getBuildParamOrDefault "target" "All"
