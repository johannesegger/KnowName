namespace Shared

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type Person = {
    DisplayName: string
    ImageUrl: string
}

module Person =
    let decoder = Decode.Auto.generateDecoder<Person>()
    let listDecoder : Decode.Decoder<Person list> = Decode.list decoder

module ClassName =
    let getRawClassName (className: string) =
        match className.IndexOf '_' with
        | -1 -> className
        | idx -> className.Substring(0, idx)

type RawGroup =
    | Teachers
    | Students of string

module RawGroup =
    let decoder = Decode.Auto.generateDecoder<RawGroup>()
    let listDecoder : Decode.Decoder<RawGroup list> = Decode.list decoder

    let toString = function
        | Teachers -> "Lehrer"
        | Students className -> ClassName.getRawClassName className
