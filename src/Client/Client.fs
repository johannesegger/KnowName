module Client

open Elmish
open Elmish.React
open Fable.Core.JsInterop

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

open Components.Main

importAll "./Styles/main.sass"

Program.mkProgram State.init State.update View.root
|> Program.withSubscription State.closeDropdownsOnDocumentClickSubscription
|> Program.withSubscription State.navigateThroughSuggestionsSubscription
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
