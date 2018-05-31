module App.View

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

[<Global>]
let self:obj = jsNative

printfn "Hello, Service Worker"
self?addEventListener("install", fun e ->
    printfn "[Service Worker] Install"
) |> ignore

self?addEventListener("fetch", fun e ->
    printfn "[Service Worker] Fetched resource %O" e?request?url
) |> ignore

self?addEventListener("beforeinstallprompt", fun e ->
    printfn "[Service Worker] Before install prompt"
) |> ignore