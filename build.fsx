#load ".fake/build.fsx/intellisense.fsx"

open System

open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

let serverPath = "./src/Server" |> Path.getFullName
let clientPath = "./src/Client" |> Path.getFullName
let deployDir = "./deploy" |> Path.getFullName

let platformTool tool winTool =
  let tool = if Environment.isUnix then tool else winTool
  tool
  |> Process.tryFindFileOnPath
  |> function Some t -> t | _ -> failwithf "%s not found" tool

let nodeTool = platformTool "node" "node.exe"
let yarnTool = platformTool "yarn" "yarn.cmd"
let dotnetCliTool = platformTool "dotnet" "dotnet.exe"

let run cmd args workingDir =
  let result =
    Process.execSimple
      (fun info ->
        { info with
            FileName = cmd
            WorkingDirectory = workingDir
            Arguments = args
        })
      TimeSpan.MaxValue
  if result <> 0 then failwithf "'%s %s' failed" cmd args

Target.create "Clean" (fun _ ->
  Shell.cleanDirs [ deployDir ]
)

Target.create "InstallClient" (fun _ ->
  printfn "Node version:"
  run nodeTool "--version" __SOURCE_DIRECTORY__
  printfn "Yarn version:"
  run yarnTool "--version" __SOURCE_DIRECTORY__
  run yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
  run dotnetCliTool "restore" clientPath
)

Target.create "RestoreServer" (fun _ -> 
  run dotnetCliTool "restore" serverPath
)

Target.create "Build" (fun _ ->
  run dotnetCliTool "build" serverPath
  run dotnetCliTool "fable webpack -- -p" clientPath
)

Target.create "Run" (fun _ ->
  let server = async {
    run dotnetCliTool "watch run" serverPath
  }
  let client = async {
    run dotnetCliTool "fable webpack-dev-server" clientPath
  }
  let browser = async {
    Threading.Thread.Sleep 5000
    Diagnostics.Process.Start "http://localhost:8080" |> ignore
  }

  [ server; client; browser]
  |> Async.Parallel
  |> Async.RunSynchronously
  |> ignore
)

Target.create "Bundle" (fun _ ->
  let serverDir = deployDir </> "Server"
  let clientDir = deployDir </> "Client"
  
  let publicDir = clientDir </> "public"
  let imageDir  = clientDir </> "Images"

  let publishArgs = sprintf "publish -c Release -o \"%s\"" serverDir
  run dotnetCliTool publishArgs serverPath

  !! "src/Client/public/**/*.*" |> Shell.copyFiles publicDir
  !! "src/Client/Images/**/*.*" |> Shell.copyFiles imageDir

  !! "src/Client/index.html"
  ++ "src/Client/*.css"
  |> Shell.copyFiles clientDir 
)

Target.create "Docker" (fun _ ->
  let imageName = "johannesegger/know-name"

  let buildArgs = sprintf "build -t %s ." imageName
  run "docker" buildArgs "."

  let tagArgs = sprintf "tag %s %s" imageName imageName
  run "docker" tagArgs "."
)

"Clean"
  ==> "InstallClient"
  ==> "Build"
  ==> "Bundle"
  ==> "Docker"

"InstallClient"
  ==> "RestoreServer"
  ==> "Run"

Target.runOrDefault "Build"