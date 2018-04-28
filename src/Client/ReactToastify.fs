// ts2fable 0.5.2
module rec ReactToastify
open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.JS

[<Import("*", "react-toastify")>]
let toastify: IExports = jsNative

let toastContainer b = Fable.Helpers.React.createElement(toastify.ToastContainer, b, [])

type [<AllowNullLiteral>] IExports =
    abstract ToastContainer: ToastContainerStatic
    /// Helper to override the global style.
    abstract style: props: styleProps -> unit
    abstract toast: Toast

type [<StringEnum>] [<RequireQualifiedAccess>] ToastType =
    | Info
    | Success
    | Warning
    | Error
    | Default

type ToastContent =
    U2<React.ReactNode, obj>

[<RequireQualifiedAccess; CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ToastContent =
    let ``ofReact.ReactNode`` v: ToastContent = v |> U2.Case1
    let ``isReact.ReactNode`` (v: ToastContent) = match v with U2.Case1 _ -> true | _ -> false
    let ``asReact.ReactNode`` (v: ToastContent) = match v with U2.Case1 o -> Some o | _ -> None
    let ofCase2 v: ToastContent = v |> U2.Case2
    let isCase2 (v: ToastContent) = match v with U2.Case2 _ -> true | _ -> false
    let asCase2 (v: ToastContent) = match v with U2.Case2 o -> Some o | _ -> None

type [<AllowNullLiteral>] styleProps =
    /// Set the default toast width. 
    /// Default: '320px' 
    abstract width: string option with get, set
    /// Set the toast color when no type is provided. 
    /// Default: '#fff'
    abstract colorDefault: string option with get, set
    /// Set the toast color when the type is INFO.
    /// Default: '#3498db'
    abstract colorInfo: string option with get, set
    /// Set the toast color when the type is SUCCESS. 
    /// Default: '#07bc0c'
    abstract colorSuccess: string option with get, set
    /// Set the toast color when the type is WARNING. 
    /// Default: '#f1c40f'
    abstract colorWarning: string option with get, set
    /// Set the toast color when the type is ERROR. 
    /// Default: '#e74c3c'
    abstract colorError: string option with get, set
    /// Set the progress bar color when no type is provided.
    /// Default: 'linear-gradient(to right, #4cd964, #5ac8fa, #007aff, #34aadc, #5856d6, #ff2d55)' 
    abstract colorProgressDefault: string option with get, set
    /// Media query to apply mobile style. 
    /// Default: 'only screen and (max-width : 480px)'
    abstract mobile: string option with get, set
    /// Set the z-index for the ToastContainer.
    /// Default: 9999
    abstract zIndex: U2<string, float> option with get, set
    /// Override the default position.
    /// Default: {
    ///    top: '1em',
    ///    left: '1em'
    /// }  
    abstract TOP_LEFT: obj option with get, set
    /// Override the default position.
    /// Default: {
    ///    top: '1em',
    ///    left: '50%'
    /// }  
    abstract TOP_CENTER: obj option with get, set
    /// Override the default position.
    /// Default: {
    ///    top: '1em',
    ///    right: '1em'
    /// }
    abstract TOP_RIGHT: obj option with get, set
    /// Override the default position.
    /// Default: {
    ///    bottom: '1em',
    ///    left: '1em'
    /// }  
    abstract BOTTOM_LEFT: obj option with get, set
    /// Override the default position.
    /// Default: {
    ///    bottom: '1em',
    ///    left: '50%'
    /// } 
    abstract BOTTOM_CENTER: obj option with get, set
    /// Override the default position.
    /// Default: {
    ///    bottom: '1em',
    ///    right: '1em'
    /// }  
    abstract BOTTOM_RIGHT: obj option with get, set

type [<AllowNullLiteral>] CommonOptions =
    /// Pause the timer when the mouse hover the toast.
    abstract pauseOnHover: bool option with get, set
    /// Remove the toast when clicked.
    abstract closeOnClick: bool option with get, set
    /// Set the delay in ms to close the toast automatically. 
    /// Use `false` to prevent the toast from closing.
    abstract autoClose: U2<float, obj> option with get, set
    /// Set the default position to use.
    /// One of: 'top-right', 'top-center', 'top-left', 'bottom-right', 'bottom-center', 'bottom-left'
    abstract position: string option with get, set
    /// Pass a custom close button. 
    /// To remove the close button pass `false`
    abstract closeButton: U2<React.ReactNode, obj> option with get, set
    /// An optional css class to set for the progress bar. It can be a glamor rule
    /// or a css class name.
    abstract progressClassName: U2<string, obj> option with get, set
    /// An optional css class to set. It can be a glamor rule
    /// or a css class name.
    abstract className: U2<string, obj> option with get, set
    /// An optional css class to set for the toast content. It can be a glamor rule
    /// or a css class name.
    abstract bodyClassName: U2<string, obj> option with get, set
    /// Show or not the progress bar.
    abstract hideProgressBar: bool option with get, set
    /// Pass a custom transition built with react-transition-group.
    // abstract transition: Transition option with get, set

type [<AllowNullLiteral>] ToastOptions =
    inherit CommonOptions
    /// Called inside componentDidMount.
    abstract onOpen: (unit -> unit) option with get, set
    /// Called inside componentWillUnMount.
    abstract onClose: (unit -> unit) option with get, set
    /// Set the toast type.
    /// One of: 'info', 'success', 'warning', 'error', 'default'.
    abstract ``type``: ToastType option with get, set

type [<AllowNullLiteral>] UpdateOptions =
    inherit ToastOptions
    /// Used to update a toast. 
    /// Pass any valid ReactNode(string, number, component)
    abstract render: ToastContent option with get, set

type [<AllowNullLiteral>] ToastContainerProps =
    inherit CommonOptions
    /// Whether or not to display the newest toast on top.
    /// Default: false
    abstract newestOnTop: bool option with get, set
    /// An optional inline style to apply.
    abstract style: obj option with get, set
    /// An optional css class to set. It can be a glamor rule
    /// or a css class name.
    abstract toastClassName: U2<string, obj> option with get, set

type [<AllowNullLiteral>] Toast =
    /// Shorthand to display toast of type 'success'.
    abstract success: content: ToastContent * ?options: ToastOptions -> float
    /// Shorthand to display toast of type 'info'.
    abstract info: content: ToastContent * ?options: ToastOptions -> float
    /// Shorthand to display toast of type 'warning'.
    abstract warn: content: ToastContent * ?options: ToastOptions -> float
    /// Shorthand to display toast of type 'error'.
    abstract error: content: ToastContent * ?options: ToastOptions -> float
    /// Check if a toast is active by passing the `toastId`.
    /// Each time you display a toast you receive a `toastId`. 
    abstract isActive: toastId: float -> bool
    /// Remove a toast. If no `toastId` is used, all the active toast
    /// will be removed.
    abstract dismiss: ?toastId: float -> unit
    /// Update an existing toast. By default, we keep the initial content and options of the toast.
    abstract update: toastId: float * ?options: UpdateOptions -> float
    /// Display a toast without a specific type.
    [<Emit "$0($1...)">] abstract Invoke: content: ToastContent * ?options: ToastOptions -> float
    /// Helper to set notification type
    abstract TYPE: obj with get, set
    /// Helper to set position
    abstract POSITION: obj with get, set

type ToastContainer =
    inherit React.ComponentClass<ToastContainerProps>

type [<AllowNullLiteral>] ToastContainerStatic =
    [<Emit "new $0($1...)">] abstract Create: unit -> ToastContainer
