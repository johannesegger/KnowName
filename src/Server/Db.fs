module Db

open System
open System.Data
open System.Data.Common
open System.IO
open MySql.Data.MySqlClient

let private execute connectionString fn = async {
    use connection = new MySqlConnection(connectionString)
    do! connection.OpenAsync() |> Async.AwaitTask
    return! fn connection
}

let private readAll (reader: DbDataReader) (mapEntry: IDataRecord -> 'a) : Async<'a list> = async {
    let rec readAll' acc = async {
        let! cont = reader.ReadAsync() |> Async.AwaitTask
        if cont
        then return! readAll' ((mapEntry reader) :: acc)
        else return acc
    }
    let! entries = readAll' []
    return List.rev entries
}

let getClassList connectionString = async {
    let fn (connection: MySqlConnection) = async {
        use command = connection.CreateCommand()
        command.CommandText <- "SELECT DISTINCT(SchoolClass) FROM pupil ORDER BY SchoolClass"
        use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask
        return! readAll reader (fun record -> record.GetString 0)
    }
    return! execute connectionString fn
}

type Student = {
    Id: string
    FirstName: string
    LastName: string
    ClassName: string
}

let getStudents connectionString (className: string) = async {
    let fn (connection: MySqlConnection) = async {
        use command = connection.CreateCommand()
        command.CommandText <- "SELECT SokratesId, FirstName1, LastName, SchoolClass FROM pupil WHERE SchoolClass = @className ORDER BY LastName, FirstName1"
        command.Parameters.AddWithValue("className", className) |> ignore
        use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask
        return! readAll reader (fun record ->
            let studentId = record.GetString 0
            let firstName = record.GetString 1
            let lastName = record.GetString 2
            let schoolClass = record.GetString 3
            {
                Id = studentId
                FirstName = firstName
                LastName = lastName
                ClassName = schoolClass
           })
    }
    return! execute connectionString fn
}

let tryGetStudent connectionString (studentId: string) = async {
    let fn (connection: MySqlConnection) = async {
        use command = connection.CreateCommand()
        command.CommandText <- "SELECT SokratesId, FirstName1, LastName, SchoolClass FROM pupil WHERE SokratesId = @studentId"
        command.Parameters.AddWithValue("studentId", studentId) |> ignore
        use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask
        let! all = readAll reader (fun record ->
            let studentId = record.GetString 0
            let firstName = record.GetString 1
            let lastName = record.GetString 2
            let schoolClass = record.GetString 3
            {
                Id = studentId
                FirstName = firstName
                LastName = lastName
                ClassName = schoolClass
           })
        return List.tryHead all
    }
    return! execute connectionString fn
}

type Teacher = {
    ShortName: string
    FirstName: string
    LastName: string
}

let getTeachers connectionString = async {
    let fn (connection: MySqlConnection) = async {
        use command = connection.CreateCommand()
        command.CommandText <- "SELECT Llogin, Lvorname, Lname FROM lehrer WHERE Ausgeschieden IS NULL AND Lname <> \"0\" ORDER BY Llogin, Lname, Lvorname"
        use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask
        return! readAll reader (fun record ->
            let shortName = record.GetString 0
            let firstName = record.GetString 1
            let lastName = record.GetString 2
            {
                ShortName = shortName
                FirstName = firstName
                LastName = lastName
           })
    }
    return! execute connectionString fn
}

let tryGetTeacher connectionString (teacherId: string) = async {
    let fn (connection: MySqlConnection) = async {
        use command = connection.CreateCommand()
        command.CommandText <- "SELECT Llogin, Lvorname, Lname FROM lehrer WHERE Ausgeschieden IS NULL AND Lname <> \"0\" AND Llogin = @teacherId"
        command.Parameters.AddWithValue("teacherId", teacherId) |> ignore
        use! reader = command.ExecuteReaderAsync() |> Async.AwaitTask
        let! all = readAll reader (fun record ->
            let shortName = record.GetString 0
            let firstName = record.GetString 1
            let lastName = record.GetString 2
            {
                ShortName = shortName
                FirstName = firstName
                LastName = lastName
           })
        return List.tryHead all
    }
    return! execute connectionString fn
}
