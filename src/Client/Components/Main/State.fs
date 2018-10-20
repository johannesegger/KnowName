module Components.Main.State

open System
open Elmish
open Fable.Core
open Fable.Core.JsInterop
open Fable.Helpers.React.Props
open Fable.Import
open Fable.PowerPack
open Fable.PowerPack.Fetch
open Fulma
open Thoth.Elmish
open Shared
module R = Fable.Helpers.React

let init() =
    Loading,
    Cmd.ofPromise (fetchAs<RawGroup list> "/api/get-groups" RawGroup.listDecoder) [] LoadDataSuccess LoadDataError

let loadGroup = function
    | Teachers ->
        promise {
            let! teachers = fetchAs<Person list> "/api/get-teachers" Person.listDecoder []
            return { Group = Teachers; Persons = teachers }
        }
    | Students className ->
        promise {
            let! students = fetchAs<Person list> (sprintf "/api/get-students/%s" className) Person.listDecoder []
            return { Group = Students className; Persons = students }
        }

let shuffle =
    let rand = Random()

    let swap (a: _[]) x y =
        let tmp = a.[x]
        a.[x] <- a.[y]
        a.[y] <- tmp

    fun l ->
        let a = Array.ofList l
        Array.iteri (fun i _ -> swap a i (rand.Next(i, Array.length a))) a
        List.ofArray a

let changeHighlightedSuggestion modifyIndex defaultItem suggestions =
    suggestions.Highlighted
    |> Option.bind (fun p ->
        suggestions.Items
        |> List.tryFindIndex ((=)p)
    )
    |> Option.bind(fun idx ->
        suggestions.Items
        |> List.tryItem (modifyIndex idx)
    )
    |> function
    | None -> defaultItem
    | x -> x

let updateView fn =
    // https://stackoverflow.com/a/34999925/1293659
    Browser.window.setTimeout(
        fun () ->
            Browser.window.requestAnimationFrame(fun dt ->
                fn()
            )
            |> ignore
        , 0
    )
    |> ignore

let rec update msg model =
    match msg, model with
    | LoadDataSuccess groups, Loading ->
        Loaded
            {
                Groups = groups |> List.map NotLoadedGroup
                GroupDropdownVisible = false
                SelectedGroup = NoSelection
                Score = 0
            },
        Cmd.none
    | LoadDataError exn, Loading ->
        LoadError exn,
        Cmd.none
    | ToggleGroupDropdownVisibility, Loaded data ->
        Loaded
            { data with
                GroupDropdownVisible = not data.GroupDropdownVisible
            },
        Cmd.none
    | SelectGroup (LoadedGroup group), Loaded data ->
        Loaded
            { data with
                SelectedGroup =
                    Selection
                        {
                            Group = group
                            RemainingPersons = shuffle group.Persons
                            CurrentGuess = ""
                            Suggestions =
                                {
                                    Items = group.Persons
                                    Highlighted = None
                                }
                        }
            },
        Cmd.none
    | SelectGroup (NotLoadedGroup group), Loaded data ->
        Loaded
            { data with
                SelectedGroup = LoadingSelection group
            },
        Cmd.ofPromise loadGroup group (LoadedGroup >> SelectGroup) LoadGroupError
    | LoadGroupError e, Loaded ({ SelectedGroup = LoadingSelection group } as data) ->
        Loaded
            { data with
                SelectedGroup = LoadingSelectionError (e, group)
            },
        Cmd.none
    | CloseDropdowns, Loaded data -> Loaded { data with GroupDropdownVisible = false }, Cmd.none
    | SubmitGuess guessedPerson, Loaded ({ SelectedGroup = Selection playingModel } as loadedModel) ->
        let remainingPersons =
            match playingModel.RemainingPersons with
            | []
            | [ _ ] -> shuffle playingModel.Group.Persons
            | _ :: tail -> tail

        let updateScore fn =
            Loaded
                { loadedModel with
                    SelectedGroup = Selection
                        {
                            playingModel with
                                RemainingPersons = remainingPersons
                                CurrentGuess = ""
                                Suggestions =
                                    { playingModel.Suggestions with
                                        Items = playingModel.Group.Persons
                                        Highlighted = None
                                    }
                        }
                    Score = fn loadedModel.Score
                }

        let currentPerson = PlayingModel.currentPerson playingModel
        let toastContent =
            R.img [ Src currentPerson.ImageUrl; Style [ Height "50px" ] ]

        let state, cmd =
            let toast =
                Toast.toast currentPerson.DisplayName
                |> Toast.icon toastContent
            if guessedPerson = Some currentPerson then
                updateScore (fun s -> s + 1), Toast.success toast
            else
                updateScore (fun s -> s - 1), Toast.error toast
        let (state', cmd') = update ScrollHighlightedSuggestionIntoView state
        state', Cmd.batch [ cmd; cmd' ]
        
    | UpdateGuess text, Loaded ({ SelectedGroup = Selection playingModel } as loadedModel) ->
        let suggestions =
            playingModel.Group.Persons
            |> List.filter (fun p -> p.DisplayName.ToUpper().Contains(text.ToUpper()))
        let highlighted =
            match playingModel.Suggestions.Highlighted with
            | Some p ->
                if suggestions |> List.contains p
                then Some p
                else List.tryHead suggestions
            | None -> List.tryHead suggestions

        Loaded
            { loadedModel with
                SelectedGroup = Selection
                    { playingModel with
                        CurrentGuess = text
                        Suggestions =
                            { playingModel.Suggestions with
                                Items = suggestions
                                Highlighted = highlighted
                            }
                    }
            },
        Cmd.ofMsg ScrollHighlightedSuggestionIntoView
    | HighlightPreviousSuggestion, Loaded ({ SelectedGroup = Selection playingModel } as loadedModel) ->
        let highlighted =
            changeHighlightedSuggestion
                (fun idx -> idx - 1)
                (List.tryLast playingModel.Suggestions.Items)
                playingModel.Suggestions
        Loaded
            { loadedModel with
                SelectedGroup =
                    Selection
                        { playingModel with
                            Suggestions =
                                { playingModel.Suggestions with Highlighted = highlighted }
                        }
            },
        Cmd.ofMsg ScrollHighlightedSuggestionIntoView
    | HighlightNextSuggestion, Loaded ({ SelectedGroup = Selection playingModel } as loadedModel) ->
        let highlighted =
            changeHighlightedSuggestion
                (fun idx -> idx + 1)
                (List.tryHead playingModel.Suggestions.Items)
                playingModel.Suggestions
        Loaded
            { loadedModel with
                SelectedGroup =
                    Selection
                        { playingModel with
                            Suggestions =
                                { playingModel.Suggestions with Highlighted = highlighted }
                        }
            },
        Cmd.ofMsg ScrollHighlightedSuggestionIntoView
    | ScrollHighlightedSuggestionIntoView, Loaded ({ SelectedGroup = Selection playingModel } as loadedModel) ->
        playingModel.Suggestions.Highlighted
        |> Option.bind (fun p -> List.tryFindIndex ((=)p) playingModel.Suggestions.Items)
        |> function
        | Some idx ->
            updateView (fun () ->
                let suggestions = Browser.document.querySelector("#suggestions")
                let isScrollable = !!suggestions?scrollHeight <> !!suggestions?offsetHeight
                if isScrollable then
                    let el = suggestions.querySelectorAll(".dropdown-item").[idx]
                    el?scrollIntoView(createObj [ "block" ==> "nearest" ]) |> ignore
            )
        | None ->
            updateView (fun () ->
                Browser.document.querySelector("#suggestions")?scrollTop <- 0.
            )

        Loaded loadedModel,
        Cmd.none
    | ResetScore, Loaded loadedModel ->
        Loaded
            { loadedModel with
                Score = 0
            },
        Cmd.none
    | message, model ->
        printfn "Model and message don't match:\nMessage: %A\nModel: %A" message model
        model, Cmd.none

let closeDropdownsOnDocumentClickSubscription _model =
    let sub dispatch =
        Browser.document.addEventListener_click (fun _ -> dispatch CloseDropdowns)
    Cmd.ofSub sub

let navigateThroughSuggestionsSubscription _model =
    let keyUp, keyDown = 38., 40.
    let sub dispatch =
        Browser.document.addEventListener_keydown
            (fun ev ->
                if ev.keyCode = keyDown then dispatch HighlightNextSuggestion
                elif ev.keyCode = keyUp then dispatch HighlightPreviousSuggestion
            )
    Cmd.ofSub sub
        