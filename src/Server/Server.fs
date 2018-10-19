open System
open System.IO
open System.Net
open System.Net.Mime
open FSharp.Control.Tasks.V2.ContextInsensitive
open FSharp.Data.Sql
open Giraffe
open Giraffe.Serialization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Net.Http.Headers
open Saturn
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Processing
open Shared
open SixLabors.Primitives
open Thoth.Json.Giraffe

let publicPath = Path.GetFullPath "../Client/public"

let port = 8085

let config (services:IServiceCollection) =
    let fableJsonSettings = Newtonsoft.Json.JsonSerializerSettings()
    fableJsonSettings.Converters.Add(Fable.JsonConverter())
    services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer fableJsonSettings) |> ignore
    services

let configureSerialization (services:IServiceCollection) =
    services.AddSingleton<IJsonSerializer>(ThothSerializer())

let getGroups() = task {
    let! classNames =
        query {
            for p in SisDb.sis2.Pupil do
            where p.SchoolClass.IsSome
            sortBy p.SchoolClass
            select (Students p.SchoolClass.Value)
            distinct
        }
        |> Seq.executeQueryAsync
        |> Async.StartAsTask
    return [
        yield Teachers
        yield! classNames
    ]
}

type Teacher = {
    ShortName: string
    FirstName: string
    LastName: string
}

type Student = {
    Id: string
    FirstName: string
    LastName: string
    ClassName: string
}

let tryGetFileInDir dir fileName =
    try
        Directory.EnumerateFiles dir
        |> Seq.tryFind (fun f -> Path.GetFileNameWithoutExtension(f).Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
    with :? DirectoryNotFoundException -> None

let tryGetTeacherImage dir (teacher: Teacher) =
    sprintf "%s_%s" teacher.LastName teacher.FirstName
    |> tryGetFileInDir dir

let tryGetStudentImage dir student =
    let classDir = Path.Combine(dir, student.ClassName)
    sprintf "%s_%s" student.LastName student.FirstName
    |> tryGetFileInDir classDir

let getTeachers imageDir = task {
    let! teachers =
        query {
            for p in SisDb.sis2.Lehrer do
            where p.Ausgeschieden.IsNone
            where p.Lname.IsSome
            where p.Lvorname.IsSome
            sortBy p.Llogin
            thenBy p.Lname
            thenBy p.Lvorname
            select {
                ShortName = p.Llogin
                LastName = p.Lname.Value
                FirstName = p.Lvorname.Value
            }
        }
        |> Seq.executeQueryAsync
        |> Async.StartAsTask

    return
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
}

let getTeacherImage imageDir teacherId = task {
    let! teacher =
        query {
            for p in SisDb.sis2.Lehrer do
            where p.Ausgeschieden.IsNone
            where (p.Llogin = teacherId)
            where p.Lname.IsSome
            where p.Lvorname.IsSome
            select {
                ShortName = p.Llogin
                LastName = p.Lname.Value
                FirstName = p.Lvorname.Value
            }
        }
        |> Seq.tryHeadAsync
        |> Async.StartAsTask

    return
        teacher
        |> Option.bind (tryGetTeacherImage imageDir)
}

let getStudents imageDir className = task {
    let! students =
        query {
            for p in SisDb.sis2.Pupil do
            where (p.SchoolClass =% className)
            where p.FirstName1.IsSome
            where p.LastName.IsSome
            sortBy p.LastName
            thenBy p.FirstName1
            select {
                Id = p.SokratesId
                FirstName = p.FirstName1.Value
                LastName = p.LastName.Value
                ClassName = p.SchoolClass.Value
            }
        }
        |> Seq.executeQueryAsync
        |> Async.StartAsTask

    return
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
}

let getStudentImage imageDir studentId = task {
    let! student =
        query {
            for p in SisDb.sis2.Pupil do
            where (p.SokratesId = studentId)
            where p.FirstName1.IsSome
            where p.LastName.IsSome
            where p.SchoolClass.IsSome
            select {
                Id = p.SokratesId
                FirstName = p.FirstName1.Value
                LastName = p.LastName.Value
                ClassName = p.SchoolClass.Value
            }
        }
        |> Seq.tryHeadAsync
        |> Async.StartAsTask

    return
        student
        |> Option.bind (tryGetStudentImage imageDir)
}

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

let tryGetQueryValue key (request: HttpRequest) =
    match request.Query.TryGetValue key with
    | (true, value) -> Some value.[0]
    | _ -> None

let getEnvVar name =
    Environment.GetEnvironmentVariable name

let getEnvVarOrFail name =
    let value = getEnvVar name
    if isNull value
    then failwithf "Environment variable \"%s\" not set" name
    else value

[<EntryPoint>]
let main argv =
    let sslCertPath = getEnvVar "SSL_CERT_PATH"
    let sslCertPassword = getEnvVar "SSL_CERT_PASSWORD"
    let teacherImageDir = getEnvVarOrFail "TEACHER_IMAGE_DIR"
    let studentImageDir = getEnvVarOrFail "STUDENT_IMAGE_DIR"

    let apiRouter = router {
        get "/api/get-groups" (fun next ctx ->
            task {
                let! data = getGroups()
                return! Successful.OK data next ctx
            }
        )

        get "/api/get-teachers" (fun next ctx ->
            task {
                let! data = getTeachers teacherImageDir
                return! Successful.OK data next ctx
            }
        )

        getf "/api/get-teacher-image/%s" (fun teacherId next ctx ->
            task {
                let! imagePath = getTeacherImage teacherImageDir teacherId
                let maxWidth = tryGetQueryValue "max-width" ctx.Request |> Option.map int
                let maxHeight = tryGetQueryValue "max-height" ctx.Request |> Option.map int
                return! readAndResizeOptImage (maxWidth, maxHeight) imagePath next ctx
            }
        )

        getf "/api/get-students/%s" (fun className next ctx ->
            task {
                let! data = getStudents studentImageDir className
                return! Successful.OK data next ctx
            }
        )

        getf "/api/get-student-image/%s" (fun studentId next ctx ->
            task {
                let! imagePath = getStudentImage studentImageDir studentId
                let maxWidth = tryGetQueryValue "max-width" ctx.Request |> Option.map int
                let maxHeight = tryGetQueryValue "max-height" ctx.Request |> Option.map int
                return! readAndResizeOptImage (maxWidth, maxHeight) imagePath next ctx
            }
        )
    }

    let app = application {
        use_router apiRouter
        memory_cache
        use_static publicPath
        service_config configureSerialization
        use_gzip
        host_config(fun host ->
            host.UseKestrel(fun options ->
                options.Listen(IPAddress.Any, port, fun listenOptions ->
#if DEBUG
                    listenOptions.UseHttps() |> ignore
#else
                    listenOptions.UseHttps(sslCertPath, sslCertPassword) |> ignore
#endif
                )
            )
        )
        app_config(fun app ->
#if DEBUG
            app.UseDeveloperExceptionPage()
#else
            // app.UseExceptionHandler("/Error")
            app.UseHsts()
#endif
        )
        service_config (fun services ->
            services.AddHttpsRedirection(fun options ->
                options.HttpsPort <- Nullable<_> 443
            )
        )
    }
    run app
    0