module Components.Main.View

open Fable.Core
open Fable.Core.JsInterop
open Fable.Helpers.React.Props
open Fulma
open Fulma.Extensions
open Fulma.FontAwesome
open Shared
module R = Fable.Helpers.React
module RAC = ReactAutocomplete

let root model dispatch =
    R.div []
        [ yield
            match model with
            | Loading ->
                PageLoader.pageLoader [ PageLoader.Color IsSuccess
                                        PageLoader.IsActive true ]
                    [ R.span [ Class Heading.Classes.Title; Style [ FontVariant "small-caps" ] ] [ R.str "Daten werden geladen" ] ]
            | Loaded data ->
                let selectedGroupName =
                    match data.SelectedGroup with
                    | NoSelection -> Option.None
                    | LoadingSelection group
                    | LoadingSelectionError (_, group) -> RawGroup.toString group |> Some
                    | Selection playingModel -> RawGroup.toString playingModel.Group.Group |> Some
                Card.card []
                    [
                      yield Card.header [ Props [ Style [ Padding "0.75rem" ] ] ]
                        [ Level.level [ Level.Level.Option.Props [ Style [ Width "100%" ] ] ]
                            [ Level.left []
                                [ Level.item []
                                    [ Heading.h5 [ Heading.IsSubtitle ]
                                        [ R.strong [] [ R.str "Gruppe:" ] ]
                                    ]
                                  Level.item []
                                    [ Dropdown.dropdown [ Dropdown.IsActive data.GroupDropdownVisible ]
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
                                    ]
                                ]
                              Level.right []
                                [ Level.item []
                                    [ Heading.h5
                                        [ Heading.IsSubtitle
                                          Heading.Props
                                            [ OnDoubleClick (fun _ev -> dispatch ResetScore)
                                              Style [ !!("userSelect", "none") ]
                                            ]
                                        ]
                                        [ R.strong [] [ R.str "Punkte: " ]
                                          R.strong [ Style (if data.Score >= 0 then [ Color "lightgreen" ] else [ Color "red" ]) ] [ sprintf "%d" data.Score |> R.str ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                      yield Card.content []
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
                        | Selection ({ RemainingPersons = currentPerson :: _ }) ->
                            [ Image.image []
                                [ R.img [
                                    Src currentPerson.ImageUrl
                                    Style [ MaxHeight "600px"; !!("objectFit", "contain") ]
                                  ]
                                ]
                            ]
                        | Selection ({ Group = group; RemainingPersons = [] }) ->
                            [ Notification.notification [ Notification.Color IsDanger ]
                                [ RawGroup.toString group.Group |> sprintf "Für keine Person der Gruppe %s ist ein Foto vorhanden" |> R.str ]
                            ]
                        )
                      match data.SelectedGroup with
                      | NoSelection
                      | LoadingSelection _
                      | LoadingSelectionError _
                      | Selection ({ RemainingPersons = [] }) -> ()
                      | Selection playingModel ->
                          yield Card.footer []
                            [ Card.Footer.item []
                                [ Container.container [ Container.IsFluid ]
                                    [ R.form [ Action "javascript:void(0);"; OnSubmit (fun _ev -> playingModel.CurrentGuess |> Option.defaultValue "" |> SubmitGuess |> dispatch) ]
                                        [ ReactPopper.manager [ ReactPopper.Tag false ]
                                            [ ReactAutocomplete.autocomplete
                                                [ RAC.Items (List.toArray playingModel.Group.Persons)
                                                  RAC.GetItemValue (fun p -> p.DisplayName)
                                                  RAC.RenderItem (fun item isHighlighted ->
                                                    R.div
                                                        [ yield Style [ Padding "5px 2px"; Cursor "pointer" ] :> IHTMLProp
                                                          if isHighlighted then yield ClassName "is-highlighted" :> IHTMLProp
                                                        ]
                                                        [ R.str item.DisplayName ]
                                                  )
                                                  RAC.RenderMenu (fun items ->
                                                    ReactPopper.popper
                                                        [ ReactPopper.Placement "right-bottom"
                                                          ReactPopper.Style (keyValueList CaseRules.LowerFirst [ Border "1px solid #ddd"; BackgroundColor "white"; Width "100%" ])
                                                        ]
                                                        items
                                                  )
                                                  RAC.RenderInput (fun props ->
                                                    ReactPopper.target [] [ R.createElement("input", props, []) ]
                                                  )
                                                  RAC.OnChange (fun _ev value -> UpdateGuess value |> dispatch)
                                                  RAC.OnSelect (fun value _item -> SubmitGuess value |> dispatch)
                                                  RAC.Value (Option.defaultValue "" playingModel.CurrentGuess)
                                                  RAC.ShouldItemRender (fun item (value: string) -> item.DisplayName.ToLower().Contains(value.ToLower()))
                                                  RAC.MenuStyle (keyValueList CaseRules.LowerFirst [ Bottom "100px" ])
                                                  RAC.InputProps [ ClassName "input is-large"; Placeholder "Name" ]
                                                ]
                                                []
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                    ]
            | LoadError _e -> R.str "Fehler beim Laden der Daten."

          let options = createEmpty<ReactToastify.ToastContainerProps>
          options.autoClose <- U2.Case1 5000. |> Some
          options.position <- Some "bottom-left"
          yield ReactToastify.toastContainer options
        ]
