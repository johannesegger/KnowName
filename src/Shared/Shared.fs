namespace Shared

type Person = {
    DisplayName: string
    ImageUrl: string
}

module ClassName =
    let getRawClassName (className: string) =
        match className.IndexOf '_' with
        | -1 -> className
        | idx -> className.Substring(0, idx)

type RawGroup =
    | Teachers
    | Students of string

module RawGroup =
    let toString = function
        | Teachers -> "Lehrer"
        | Students className -> ClassName.getRawClassName className
