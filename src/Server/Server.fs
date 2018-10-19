open System
open System.IO
open System.Net.Mime
open FSharp.Data.Sql
open Giraffe
open Giraffe.Serialization
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Net.Http.Headers
open Saturn
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Processing
open Shared
open SixLabors.Primitives

let clientPath = Path.Combine("..","Client") |> Path.GetFullPath

let port = 8085us

let config (services:IServiceCollection) =
    let fableJsonSettings = Newtonsoft.Json.JsonSerializerSettings()
    fableJsonSettings.Converters.Add(Fable.JsonConverter())
    services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer fableJsonSettings) |> ignore
    services

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
    Directory.EnumerateFiles dir
    |> Seq.tryFind (fun f -> Path.GetFileNameWithoutExtension(f).Equals(fileName, StringComparison.InvariantCultureIgnoreCase))

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

let browserRouter = scope {
    get "/" (htmlFile (Path.Combine(clientPath, "index.html")))
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

let apiRouter (teacherImageDir, studentImageDir) = scope {
    get "/get-groups" (fun next ctx ->
        task {
            let! data = getGroups()
            return! Successful.OK data next ctx
        }
    )

    get "/get-teachers" (fun next ctx ->
        task {
            let! data = getTeachers teacherImageDir
            return! Successful.OK data next ctx
        }
    )

    getf "/get-teacher-image/%s" (fun teacherId next ctx ->
        task {
            let! imagePath = getTeacherImage teacherImageDir teacherId
            let maxWidth = tryGetQueryValue "max-width" ctx.Request |> Option.map int
            let maxHeight = tryGetQueryValue "max-height" ctx.Request |> Option.map int
            return! readAndResizeOptImage (maxWidth, maxHeight) imagePath next ctx
        }
    )

    getf "/get-students/%s" (fun className next ctx ->
        task {
            let! data = getStudents studentImageDir className
            return! Successful.OK data next ctx
        }
    )

    getf "/get-student-image/%s" (fun studentId next ctx ->
        task {
            let! imagePath = getStudentImage studentImageDir studentId
            let maxWidth = tryGetQueryValue "max-width" ctx.Request |> Option.map int
            let maxHeight = tryGetQueryValue "max-height" ctx.Request |> Option.map int
            return! readAndResizeOptImage (maxWidth, maxHeight) imagePath next ctx
        }
    )
}

let mainRouter imageDirs = scope {
    forward "" browserRouter
    forward "/api" (apiRouter imageDirs)
}

let app imageDirs = application {
    router (mainRouter imageDirs)
    url ("http://0.0.0.0:" + port.ToString() + "/")
    memory_cache
    use_static clientPath
    service_config config
    use_gzip
}

[<EntryPoint>]
let main argv =
    let tryGetEnvVar key =
        match Environment.GetEnvironmentVariable key with
        | x when String.IsNullOrEmpty x -> None
        | x -> Some x

    match tryGetEnvVar "TEACHER_IMAGES_PATH", tryGetEnvVar "STUDENT_IMAGES_PATH" with
    | Some teacherImageDir, Some studentImageDir ->
        app (teacherImageDir, studentImageDir) |> run
        0
    | _ ->
        eprintfn "ERROR: environment variables `TEACHER_IMAGES_PATH` and `STUDENT_IMAGES_PATH` not set"
        1