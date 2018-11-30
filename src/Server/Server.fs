open System
open System.IO
open System.Net
open System.Net.Mime
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Giraffe.Serialization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Net.Http.Headers
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Processing
open SixLabors.Primitives
open Thoth.Json.Giraffe
open Shared

let publicPath = Path.GetFullPath "../Client/public"

let configureSerialization (services:IServiceCollection) =
    services.AddSingleton<IJsonSerializer>(ThothSerializer())

let tryGetQueryValue key (request: HttpRequest) =
    match request.Query.TryGetValue key with
    | (true, value) -> Some value.[0]
    | _ -> None

let resizeFile (maxWidth, maxHeight) (path: string) =
    let limit value max =
        if value > max
        then max
        else value
    let size =
        match maxWidth, maxHeight with
        | Some width, Some height -> Size(limit width 1000, limit height 1000) |> Some
        | Some width, None -> Size(limit width 1000, 0) |> Some
        | None, Some height -> Size(0, limit height 1000) |> Some
        | None, None -> None
    match size with
    | Some size ->
        use image = Image.Load path
        image.Mutate(fun ctx ->
            ResizeOptions(
                Size = size,
                Mode = ResizeMode.Max
            )
            |> ctx.Resize
            |> ignore
        )
        use stream = new MemoryStream()
        image.Save(stream, ImageFormats.Jpeg)
        stream.ToArray()
    | None -> File.ReadAllBytes path

let readAndResizeOptImage size = function
    | Some path ->
        let handler =
            setHttpHeader HeaderNames.ContentType MediaTypeNames.Image.Jpeg
            >=> setBody (resizeFile size path)
        Successful.ok handler
    | None ->
        RequestErrors.notFound (fun fn ctx -> fn ctx)

let getGroups classList : HttpHandler =
    fun next ctx ->
        task {
            let! classNames = Async.StartAsTask classList
            let groups = [
                yield Teachers
                yield! List.map Students classNames
            ]
            return! Successful.OK groups next ctx
        }

let tryGetFileInDir dir fileName =
    try
        Directory.EnumerateFiles dir
        |> Seq.tryFind (fun f -> Path.GetFileNameWithoutExtension(f).Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
    with :? DirectoryNotFoundException -> None

let tryGetTeacherImage dir (teacher: Db.Teacher) =
    sprintf "%s_%s" teacher.LastName teacher.FirstName
    |> tryGetFileInDir dir

let tryGetStudentImage dir (student: Db.Student) =
    let classDir = Path.Combine(dir, student.ClassName)
    sprintf "%s_%s" student.LastName student.FirstName
    |> tryGetFileInDir classDir

let getTeachers imageDir teachers : HttpHandler =
    fun next ctx ->
        task {
            let! teachers = Async.StartAsTask teachers
            let persons = 
                teachers
                |> Seq.choose (fun teacher ->
                    match tryGetTeacherImage imageDir teacher with
                    | Some _ ->
                        {
                            DisplayName = sprintf "%s - %s %s" teacher.ShortName (teacher.LastName.ToUpper()) teacher.FirstName
                            ImageUrl = sprintf "/api/get-teacher-image/%s" teacher.ShortName
                        }
                        |> Some
                    | None ->
                        // printfn "Can't find image for %s %s" p.LastName p.FirstName
                        None
                )
                |> Seq.toList
            return! Successful.OK persons next ctx
        }

let getTeacherImage imageDir tryGetTeacher teacherId : HttpHandler =
    fun next ctx ->
        task {
            let! teacher = tryGetTeacher teacherId |> Async.StartAsTask
            let imagePath =
                teacher
                |> Option.bind (tryGetTeacherImage imageDir)
            let maxWidth = tryGetQueryValue "max-width" ctx.Request |> Option.map int
            let maxHeight = tryGetQueryValue "max-height" ctx.Request |> Option.map int
            return! readAndResizeOptImage (maxWidth, maxHeight) imagePath next ctx
        }

let getStudents imageDir getStudents className : HttpHandler = 
    fun next ctx ->
        task {
            let! students = getStudents className
            let persons =
                students
                |> Seq.choose (fun student ->
                    match tryGetStudentImage imageDir student with
                    | Some _ ->
                        {
                            DisplayName = sprintf "%s %s" (student.LastName.ToUpper()) student.FirstName
                            ImageUrl = sprintf "/api/get-student-image/%s" student.Id
                        }
                        |> Some
                    | None ->
                        // printfn "Can't find image for %s %s" student.LastName student.FirstName
                        None
                )
                |> Seq.toList
            return! Successful.OK persons next ctx
        }

let getStudentImage imageDir tryGetStudent studentId : HttpHandler =
    fun next ctx ->
        task {
            let! student = tryGetStudent studentId |> Async.StartAsTask
            let imagePath =
                student
                |> Option.bind (tryGetStudentImage imageDir)
            let maxWidth = tryGetQueryValue "max-width" ctx.Request |> Option.map int
            let maxHeight = tryGetQueryValue "max-height" ctx.Request |> Option.map int
            return! readAndResizeOptImage (maxWidth, maxHeight) imagePath next ctx
        }

let getEnvVar name =
    Environment.GetEnvironmentVariable name

let getEnvVarOrFail name =
    let value = getEnvVar name
    if isNull value
    then failwithf "Environment variable \"%s\" not set" name
    else value

[<EntryPoint>]
let main argv =
    let sslCertPath = getEnvVarOrFail "SSL_CERT_PATH"
    let sslCertPassword = getEnvVar "SSL_CERT_PASSWORD"
    let connectionString = getEnvVarOrFail "SISDB_CONNECTION_STRING"
    let teacherImageDir = getEnvVarOrFail "TEACHER_IMAGE_DIR"
    let studentImageDir = getEnvVarOrFail "STUDENT_IMAGE_DIR"
    let classList = Db.getClassList connectionString
    let teachers = Db.getTeachers connectionString
    let tryGetTeacher = Db.tryGetTeacher connectionString
    let students = Db.getStudents connectionString
    let tryGetStudent = Db.tryGetStudent connectionString

    let webApp = choose [
        GET >=> choose [
            route "/api/get-groups" >=> getGroups classList
            route "/api/get-teachers" >=> getTeachers teacherImageDir teachers
            routef "/api/get-teacher-image/%s" (getTeacherImage teacherImageDir tryGetTeacher) 
            routef "/api/get-students/%s" (getStudents studentImageDir students)
            routef "/api/get-student-image/%s" (getStudentImage studentImageDir tryGetStudent)
        ]
    ]

    let errorHandler (ex : Exception) (logger : ILogger) =
        logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
        clearResponse >=> setStatusCode 500 >=> text ex.Message

    let configureApp (app : IApplicationBuilder) =
        let env = app.ApplicationServices.GetService<IHostingEnvironment>()
        match env.IsDevelopment() with
        | true  -> app.UseDeveloperExceptionPage() |> ignore
        | false -> app.UseGiraffeErrorHandler errorHandler |> ignore
        app
            .UseHttpsRedirection()
            .UseDefaultFiles()
            .UseStaticFiles()
            // .UseAuthentication()
            .UseGiraffe(webApp)

    let configureServices (services : IServiceCollection) =
        services.AddGiraffe() |> ignore
        services.AddSingleton<IJsonSerializer>(ThothSerializer()) |> ignore

    let configureLogging (ctx: WebHostBuilderContext) (builder : ILoggingBuilder) =
        builder
            .AddFilter(fun l -> ctx.HostingEnvironment.IsDevelopment() || l.Equals LogLevel.Error)
            .AddConsole()
            .AddDebug() |> ignore

    WebHostBuilder()
        .UseKestrel(fun options ->
            options.ListenAnyIP 5000
            options.ListenAnyIP(5001, fun listenOptions ->
                listenOptions.UseHttps(sslCertPath, sslCertPassword) |> ignore
            )
        )
        .UseWebRoot(publicPath)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()
    0