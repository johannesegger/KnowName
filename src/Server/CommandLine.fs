module CommandLine
open System

let tryGetArg (argv: string array) name =
    argv
    |> Array.skipWhile (fun arg -> not <| arg.TrimStart('-').Equals(name, StringComparison.InvariantCultureIgnoreCase))
    |> Array.tryItem 1
