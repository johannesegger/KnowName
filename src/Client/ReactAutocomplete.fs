module ReactAutocomplete

open Fable.Core.JsInterop
open Fable.Import.React
open Fable.Helpers.React.Props
open Fable.Core

type AutocompleteProp<'TItem, 'TValue> =
    | Items of 'TItem array
    | GetItemValue of ('TItem -> 'TValue)
    | RenderItem of ('TItem -> bool -> ReactElement)
    | RenderMenu of (ReactElement list -> ReactElement)
    | RenderInput of (obj -> ReactElement)
    | OnChange of (FormEvent -> 'TValue -> unit)
    | OnSelect of ('TValue -> 'TItem -> unit)
    | Value of 'TValue
    | ShouldItemRender of ('TItem -> 'TValue -> bool)
    | MenuStyle of obj
    | InputProps of obj
    interface IHTMLProp

let private Autocomplete: ComponentClass<obj> = importDefault "react-autocomplete/build/lib/index"

let autocomplete (b: IHTMLProp list) c =
    Fable.Helpers.React.from
        Autocomplete
        (keyValueList CaseRules.LowerFirst b)
        c
