#r @"packages/build/FAKE/tools/FakeLib.dll"

open System

open Fake

let rootPath = __SOURCE_DIRECTORY__
let serverPath = rootPath @@ "./src/Server"
let clientPath = rootPath @@ "./src/Client"
let serviceWorkerPath = rootPath @@ "./src/ServiceWorker"
let deployDir = rootPath @@ "./deploy"

let platformTool tool winTool =
  let tool = if isUnix then tool else winTool
  tool
  |> ProcessHelper.tryFindFileOnPath
  |> function Some t -> t | _ -> failwithf "%s not found" tool

let nodeTool = platformTool "node" "node.exe"
let yarnTool = platformTool "yarn" "yarn.cmd"

let dotnetcliVersion = DotNetCli.GetDotNetSDKVersionFromGlobalJson()
let mutable dotnetCli = "dotnet"

let run cmd args workingDir =
  let result =
    ExecProcess (fun info ->
      info.FileName <- cmd
      info.WorkingDirectory <- workingDir
      info.Arguments <- args) TimeSpan.MaxValue
  if result <> 0 then failwithf "'%s %s' failed" cmd args

Target "Clean" (fun _ -> 
  CleanDirs [deployDir]
)

Target "InstallDotNetCore" (fun _ ->
  dotnetCli <- DotNetCli.InstallDotNetSDK dotnetcliVersion
)

Target "InstallClient" (fun _ ->
  printfn "Node version:"
  run nodeTool "--version" rootPath
  printfn "Yarn version:"
  run yarnTool "--version" rootPath
  run yarnTool "install --frozen-lockfile" rootPath
  run dotnetCli "restore" clientPath
  run dotnetCli "restore" serviceWorkerPath
)

Target "RestoreServer" (fun () -> 
  run dotnetCli "restore" serverPath
)

Target "Build" (fun () ->
  run dotnetCli "build" serverPath
  run dotnetCli "fable webpack -- -p" clientPath//rootPath
)

Target "Run" (fun () ->
  let server = async {
    run dotnetCli "watch run" serverPath
  }
  let client = async {
    run dotnetCli "fable webpack-dev-server" clientPath
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

Target "Bundle" (fun _ ->
  let serverDir = deployDir </> "Server"
  let clientDir = deployDir </> "Client"
  
  let publicDir = clientDir </> "public"
  let imageDir  = clientDir </> "Images"

  let publishArgs = sprintf "publish -c Release -o \"%s\"" serverDir
  run dotnetCli publishArgs serverPath

  !! "src/Client/public/**/*.*" |> CopyFiles publicDir
  !! "src/Client/Images/**/*.*" |> CopyFiles imageDir

  !! "src/Client/index.html"
  ++ "src/Client/manifest.webmanifest"
  ++ "src/Client/*.css"
  |> CopyFiles clientDir 
)

let dockerUser = "safe-template"
let dockerImageName = "safe-template"

let dockerFullName = sprintf "%s/%s" dockerUser dockerImageName

Target "Docker" (fun _ ->
  let buildArgs = sprintf "build -t %s ." dockerFullName
  run "docker" buildArgs "."

  let tagArgs = sprintf "tag %s %s" dockerFullName dockerFullName
  run "docker" tagArgs "."
)

"Clean"
  ==> "InstallDotNetCore"
  ==> "InstallClient"
  ==> "Build"
  ==> "Bundle"
  ==> "Docker"

"InstallClient"
  ==> "RestoreServer"
  ==> "Run"

RunTargetOrDefault "Build"