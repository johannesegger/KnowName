module SisDb

open System
open FSharp.Data.Sql

[<Literal>]
let private ConnectionString = "Server=localhost;Port=8081;Database=sis2;User=root;Password=1234"

[<Literal>]
let private ResolutionPath = __SOURCE_DIRECTORY__ + @"\..\..\packages\server\MySql.Data\lib\net452"

type private SisSqlDataProvider =
    SqlDataProvider< 
        ConnectionString = ConnectionString,
        DatabaseVendor = Common.DatabaseProviderTypes.MYSQL,
        ResolutionPath = ResolutionPath,
        UseOptionTypes = true>
let private ctx =
    let connectionString = Environment.GetEnvironmentVariable "SISDB_CONNECTION_STRING"
    SisSqlDataProvider.GetDataContext connectionString

let sis2 = ctx.Sis2