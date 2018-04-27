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

type LoadedGroup = {
    Group: RawGroup
    Persons: Person list
}

type Group =
    | NotLoadedGroup of RawGroup
    | LoadedGroup of LoadedGroup

module Group =
    let toString = function
        | NotLoadedGroup g
        | LoadedGroup { Group = g } ->
            RawGroup.toString g

type PlayingModel = {
    Group: LoadedGroup
    RemainingPersons: Person list
    CurrentGuess: string option
}

module PlayingModel =
    let currentPerson model = List.head model.RemainingPersons

type SelectedGroupModel =
    | NoSelection
    | LoadingSelection of RawGroup
    | LoadingSelectionError of exn * RawGroup
    | Selection of PlayingModel

type LoadedModel = {
    Groups: Group list
    GroupDropdownVisible: bool
    SelectedGroup: SelectedGroupModel
    Score: int
}

type Model =
    | Loading
    | LoadError of exn
    | Loaded of LoadedModel

type Msg =
    | LoadDataSuccess of RawGroup list
    | LoadDataError of exn
    | ToggleGroupDropdownVisibility
    | SelectGroup of Group
    | LoadGroupSuccess of LoadedGroup
    | LoadGroupError of exn
    | CloseDropdowns
    | SubmitGuess of string
    | UpdateGuess of string
    | ResetScore

type GuessResult =
    | Correct
    | Incorrect of Person list
    | Skipped

