// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#I "packages/FAKE/tools"
#r "packages/FAKE/tools/FakeLib.dll"

open System
open System.IO
open System.Text.RegularExpressions
open Fake 
open Fake.AssemblyInfoFile
open Fake.Git

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

let files includes = 
  { BaseDirectory = __SOURCE_DIRECTORY__
    Includes = includes
    Excludes = [] } 

// Information about the project to be used at NuGet and in AssemblyInfo files
let project = "FSharp.Charting"
let authors = ["Carl Nolan, Tomas Petricek"]
let summary = "A Charting Library for F#"
let description = """
  The FSharp.Charting library (FSharp.Charting.dll) is a compositional library for creating charts
  from F#. It is designed to be a great fit for data scripting in F# Interactive, but 
  charts can also be embedded in Windows applications. The library is a wrapper for .NET Chart
  Controls, which are only supported on Windows."""
let tags = "F# FSharpChart charting plotting visualization"

// Information for the Gtk build of the project 
let projectGtk = "FSharp.Charting.Gtk"
let summaryGtk = summary + " (Gtk, cross-platform)"
let tagsGtk = tags + " Gtk GtkSharp OxyPlot"
let descriptionGtk = """
  The FSharp.Charting library (FSharp.Charting.Gtk.dll) is a cross-platform variation of
  of FSharp.Charting. It can be used on Windows, OSX and other platforms supporting Gtk."""

// Read additional information from the release notes document
let release = 
    File.ReadLines "RELEASE_NOTES.md" 
    |> ReleaseNotesHelper.parseReleaseNotes

let version = release.AssemblyVersion
let releaseNotes = release.Notes |> String.concat "\n"

let compilingOnUnix =
    match Environment.OSVersion.Platform with
    | PlatformID.MacOSX | PlatformID.Unix -> true
    | _ -> false

// --------------------------------------------------------------------------------------
// Generate assembly info files with the right version & up-to-date information

Target "AssemblyInfo" (fun _ ->
    [ ("FSharp.Charting/AssemblyInfo.fs", "FSharp.Charting", project, summary)
      ( "FSharp.Charting.Gtk/AssemblyInfo.Gtk.fs", "FSharp.Charting.Gtk", projectGtk, summaryGtk ) ]
    |> Seq.iter (fun (fileName, title, project, summary) ->
        CreateFSharpAssemblyInfo fileName
           [ Attribute.Title title
             Attribute.Product project
             Attribute.Description summary
             Attribute.Version version
             Attribute.FileVersion version] )
)

// --------------------------------------------------------------------------------------
// Update the assembly version numbers in the script file.

Target "UpdateFsxVersions" (fun _ ->
    for nm in [ "FSharp.Charting"; "FSharp.Charting.Gtk" ] do
        for path in [ @"./" + nm + "/" + nm + ".fsx" ] @ Seq.toList (Directory.EnumerateFiles "docs/content")  do
          let text1 = File.ReadAllText(path)
          // Adjust entries like #I "../../../packages/FSharp.Charting.0.84"
          let text2 = Regex.Replace(text1, "packages/" + nm + @".(.*)/lib/net40""", "packages/" + nm + sprintf @".%s/lib/net40""" version)
          // Adjust entries like #load "packages/FSharp.Charting.0.84/FSharp.Charting.fsx"
          let text3 = Regex.Replace(text2, "packages/" + nm + @".(.*)/" + nm + ".fsx", "packages/" + nm + sprintf @".%s/" version + nm + ".fsx")
          if text1 <> text3 then 
              File.WriteAllText(path, text3)
)

// --------------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ ->
    CleanDirs ["bin"]
    CleanDirs ["docs/output"]
    CleanDirs ["temp"]
)

// --------------------------------------------------------------------------------------
// Build library (builds Visual Studio solution, which builds multiple versions
// of the runtime library & desktop + Silverlight version of design time library)

Target "Build" (fun _ ->
    (files [if not compilingOnUnix then
                yield "FSharp.Charting/FSharp.Charting.fsproj";
                yield "tests/FSharp.Charting.Tests.fsproj" ])
    |> MSBuildRelease "" "Rebuild"
    |> ignore
)

Target "BuildGtk" (fun _ ->
    (files [ "FSharp.Charting.Gtk/FSharp.Charting.Gtk.fsproj" ])
    |> MSBuildRelease "" "Rebuild"
    |> ignore
)
// --------------------------------------------------------------------------------------
// Run the unit tests using test runner & kill test runner when complete

Target "RunTests" (fun _ ->
    ActivateFinalTarget "CloseTestRunner"

    if not compilingOnUnix then
      (files [ "tests/bin/Release/FSharp.Charting.Tests.dll"])
      |> NUnit (fun p ->
        { p with
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
    let descriptionGtk = descriptionGtk.Replace("\r", "").Replace("\n", "").Replace("  ", " ")

    if not compilingOnUnix then
      NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = version
            ReleaseNotes = releaseNotes
            Tags = tags
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] 
            WorkingDir = "./nuget" })
        "nuget/FSharp.Charting.nuspec"

    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = projectGtk
            Summary = summaryGtk
            Description = descriptionGtk
            Version = version
            ReleaseNotes = releaseNotes
            Tags = tagsGtk
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] 
            WorkingDir = "./nuget" })
        "nuget/FSharp.Charting.Gtk.nuspec"

)

// --------------------------------------------------------------------------------------
// Release Scripts

Target "GenerateDocs" (fun _ ->
    executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"] [] |> ignore
)

Target "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    if not (System.IO.Directory.Exists tempDocsDir) then 
        Repository.cloneSingleBranch "" "https://github.com/fslaborg/FSharp.Charting.git" "gh-pages" tempDocsDir

    fullclean tempDocsDir
    CopyRecursive "docs/output" tempDocsDir true |> tracefn "%A"
    StageAll tempDocsDir
    Commit tempDocsDir (sprintf "Update generated documentation for version %s" version) 
    Branches.push "temp/gh-pages"
)


Target "Release" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build target=<Target>' to verride

Target "All" DoNothing

"Clean"
  ==> "AssemblyInfo"
  ==> "UpdateFsxVersions"
  ==> "Build"
  ==> "RunTests"

"UpdateFsxVersions"
  ==> "BuildGtk"

"Build"  ==> "NuGet"
"BuildGtk"  ==> "NuGet"

"NuGet"
  ==> "All"

"All" ==> "Release"
"GenerateDocs" ==> "ReleaseDocs" ==> "Release"

Run <| getBuildParamOrDefault "target" "All"
