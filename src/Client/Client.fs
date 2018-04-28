module Client

open Elmish
open Elmish.React

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

open Client.Components.Main

Program.mkProgram State.init State.update View.root
|> Program.withSubscription State.closeDropdownsOnDocumentClickSubscription
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
