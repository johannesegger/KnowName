module ReactPopper

open Fable.Core
open Fable.Core.JsInterop
open Fable.Helpers.React.Props
open Fable.Import.React

type ManagerProp =
    | Tag of obj
    interface IHTMLProp

let private Manager: ComponentClass<obj> = importMember "react-popper"

let manager (b: IHTMLProp list) c =
    Fable.Helpers.React.from
        Manager
        (keyValueList CaseRules.LowerFirst b)
        c

type PopperProp =
    | Placement of string
    | Style of obj
    interface IHTMLProp

let private Popper: ComponentClass<obj> = importMember "react-popper"

let popper (b: IHTMLProp list) c =
    Fable.Helpers.React.from
        Popper
        (keyValueList CaseRules.LowerFirst b)
        c

let private Target: ComponentClass<obj> = importMember "react-popper"

let target (b: IHTMLProp list) c =
    Fable.Helpers.React.from
        Target
        (keyValueList CaseRules.LowerFirst b)
        c
