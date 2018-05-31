module Client

open Elmish
open Elmish.React
open Fable.Core.JsInterop

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

open Components.Main
open Fable.Import.Browser
open Fable.PowerPack

importAll "./Styles/main.sass"

if not <| isNull navigator.serviceWorker then
    printfn "Registering service worker"
    navigator.serviceWorker.register "./sw.js"
    |> Promise.start
else
    eprintfn "Cannot register service worker"

Program.mkProgram State.init State.update View.root
|> Program.withSubscription State.closeDropdownsOnDocumentClickSubscription
|> Program.withSubscription State.navigateThroughSuggestions
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
