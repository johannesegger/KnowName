module Components.Main.View

open Fable.Core
open Fable.Core.JsInterop
open Fable.Helpers.React.Props
open Fulma
open Fulma.Extensions
open Fulma.FontAwesome
open Shared
open Fulma
open Fable
module R = Fable.Helpers.React
module RAC = ReactAutocomplete

let groupDropdown data dispatch =
    let selectedGroupName =
        match data.SelectedGroup with
        | NoSelection -> Option.None
        | LoadingSelection group
        | LoadingSelectionError (_, group) -> RawGroup.toString group |> Some
        | Selection playingModel -> RawGroup.toString playingModel.Group.Group |> Some
    Dropdown.dropdown [ Dropdown.IsActive data.GroupDropdownVisible ]
        [ R.div [ Class "dropdown-trigger" ]
            [ Button.button [ Button.OnClick (fun _ -> dispatch ToggleGroupDropdownVisibility) ]
                [ R.span []
                    [ R.str (selectedGroupName |> Option.defaultValue "Bitte wählen") ]
                  Icon.faIcon [ Icon.Size IsSmall ] [ Fa.icon Fa.I.AngleDown ]
                ]
            ]
          Dropdown.menu [ ]
            [ Dropdown.content []
                [
                    for group in data.Groups do
                        let groupName = Group.toString group
                        yield Dropdown.Item.a
                            [
                                Dropdown.Item.Option.Props [
                                    OnClick (fun _ev -> dispatch (SelectGroup group))
                                ]
                                Dropdown.Item.IsActive (selectedGroupName = Some groupName)
                            ]
                            [ R.str groupName ]
                ]
            ]
        ]

let root model dispatch =
    R.div []
        [ yield
            match model with
            | Loading ->
                PageLoader.pageLoader [ PageLoader.Color IsSuccess
                                        PageLoader.IsActive true ]
                    [ R.span [ Class Heading.Classes.Title; Style [ FontVariant "small-caps" ] ] [ R.str "Daten werden geladen" ] ]
            | Loaded data ->
                Container.container []
                    [
                      yield R.div [ Style [ Padding "0.75rem" ] ]
                        [ Level.level [ Level.Level.Option.Props [ Style [ Width "100%" ] ] ]
                            [ Level.item [ Level.Item.HasTextCentered ]
                                [ R.div []
                                    [ Level.heading [] [ R.str "Gruppe" ]
                                      R.div [ ClassName Level.Classes.Item.Title; Style [ TextAlign "left" ] ]
                                        [ groupDropdown data dispatch ]
                                    ]
                                ]
                              Level.item [ Level.Item.HasTextCentered ]
                                [
                                  match data.SelectedGroup with
                                  | NoSelection
                                  | LoadingSelection _
                                  | LoadingSelectionError _
                                  | Selection ({ RemainingPersons = [] }) -> ()
                                  | Selection playingModel ->
                                      yield
                                        R.div []
                                          [ Level.heading [ Props [ Style [ Opacity 0 ] ] ] [ R.str "Name" ] // TODO &nbsp; would suffice
                                            R.form [ Action "javascript:void(0);"; OnSubmit (fun _ev -> dispatch (SubmitGuess playingModel.Suggestions.Highlighted)) ]
                                                [ Input.text
                                                    [ Input.Placeholder "Name"
                                                      Input.OnChange (fun ev -> UpdateGuess !!ev.target?value |> dispatch)
                                                      Input.Value playingModel.CurrentGuess
                                                      Input.Props [ AutoComplete "new-password" ]
                                                    ]
                                                ]
                                          ]
                                ]
                              Level.item [ Level.Item.HasTextCentered ]
                                [ R.div
                                    [ OnDoubleClick (fun _ev -> dispatch ResetScore)
                                      ClassName Modifier.Classes.Helpers.IsUnselectable
                                    ]
                                    [ Level.heading [] [ R.str "Punkte" ]
                                      Level.title [ Props [ Style (if data.Score >= 0 then [ Color "lightgreen" ] else [ Color "red" ]) ] ]
                                        [ string data.Score |> R.str ]
                                    ]
                                ]
                            ]
                        ]
                      yield!
                        (match data.SelectedGroup with
                        | NoSelection -> []
                        | LoadingSelection group ->
                            [ PageLoader.pageLoader [ PageLoader.Color IsSuccess
                                                      PageLoader.IsActive true ]
                                [ R.span
                                    [ Class Heading.Classes.Title; Style [ FontVariant "small-caps" ] ]
                                    [ RawGroup.toString group |> sprintf "Daten für %s werden geladen" |> R.str ]
                                ]
                            ]
                        | LoadingSelectionError (e, group) ->
                            [ Notification.notification [ Notification.Color IsDanger ]
                                [ RawGroup.toString group |> sprintf "Fehler beim Laden der Gruppe %s" |> R.str ]
                            ]
                        | Selection ({ RemainingPersons = currentPerson :: _ } as playingModel) ->
                            [ Container.container [ Container.Props [ Style [ Display "flex"; Height "calc(100vh - 100px)" ] ] ]
                                [ Tile.ancestor []
                                    [ Tile.parent [ Tile.Size Tile.Is8 ]
                                        [ Tile.child []
                                            [ Box.box' [ Props [ Style [ Height "100%" ] ] ]
                                                [ Image.image [ Image.Props [ Style [ Height "100%" ] ] ]
                                                    [ R.img
                                                        [ Src currentPerson.ImageUrl
                                                          Style [ MaxHeight "100%"; ObjectFit "contain" ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                      Tile.parent [ Tile.Props [ Style [ MinHeight "auto" ] ] ]
                                        [
                                          Tile.child [ Tile.Props [ Style [ Height "100%" ] ] ]
                                            [ Box.box' [ Common.Props [ Id "suggestions"; Style [ Height "100%"; OverflowY "auto" ] ] ]
                                                [
                                                    for p in playingModel.Suggestions.Items do
                                                    yield
                                                        Dropdown.Item.a
                                                          [ Dropdown.Item.IsActive (playingModel.Suggestions.Highlighted = Some p)
                                                            Dropdown.Item.Props
                                                              [ OnClick (fun _ev -> SubmitGuess (Some p) |> dispatch) ]
                                                          ]
                                                          [ R.str p.DisplayName ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        | Selection ({ Group = group; RemainingPersons = [] }) ->
                            [ Notification.notification [ Notification.Color IsDanger ]
                                [ RawGroup.toString group.Group |> sprintf "Für keine Person der Gruppe %s ist ein Foto vorhanden" |> R.str ]
                            ]
                        )
                    ]
            | LoadError _e -> R.str "Fehler beim Laden der Daten."
        ]
