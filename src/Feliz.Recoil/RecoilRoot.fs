﻿namespace Feliz.Recoil

open Browser.WebStorage
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open System.ComponentModel

[<AutoOpen;EditorBrowsable(EditorBrowsableState.Never);Erase>]
module RecoilRoot =
    [<AutoOpen;EditorBrowsable(EditorBrowsableState.Never);Erase>]
    module Types =
        type IRootProperty = interface end
        type ITimeTravelProperty = interface end

        type TimeTravelProps =
            abstract maxHistory: int option

        type RootProps =
            abstract children: ReactElement list
            abstract logging: bool option
            abstract initializer: (MutableSnapshot -> unit) option
            abstract timeTravel: TimeTravelProps option
            abstract useLocalStorage: (Storage.Hydrator -> unit) option
            abstract useSessionStorage: (Storage.Hydrator -> unit) option
    
    [<Erase;RequireQualifiedAccess;EditorBrowsable(EditorBrowsableState.Never)>]
    module Interop =
        let inline mkRootAttr (key: string) (value: obj) = unbox<IRootProperty>(key, value)
        let inline mkTimeTravelAttr (key: string) (value: obj) = unbox<ITimeTravelProperty>(key, value)
    
    [<Erase>]
    type root =
        static member inline children (children: ReactElement list) = Interop.mkRootAttr  "children" children

        /// Enables logging for any atoms with a set persistence type.
        ///
        /// Similar to React.StrictMode, this will do nothing in production mode.
        ///
        /// This will be adjusted later, see: https://github.com/facebookexperimental/Recoil/issues/277
        static member inline log (value: bool) = Interop.mkRootAttr "logging" value

        /// A function that will be called when the root is first rendered, 
        /// which can set initial values for atoms.
        static member inline init (initializer: MutableSnapshot -> unit) = Interop.mkRootAttr "initializer" initializer

        /// Allows you to hydrate atoms from the your local storage, those atoms 
        /// will then be observed and the local storage will be written to on any 
        /// state changes.
        static member inline localStorage (initializer: Storage.Hydrator -> unit) = Interop.mkRootAttr "useLocalStorage" initializer

        /// Allows you to hydrate atoms from the your session storage, those atoms 
        /// will then be observed and the session storage will be written to on any 
        /// state changes.
        static member inline sessionStorage (initializer: Storage.Hydrator -> unit) = Interop.mkRootAttr "useSessionStorage" initializer

        /// Enable time traveling via the useTimeTravel hook in children.
        static member inline timeTravel (value: bool) = Interop.mkRootAttr "timeTravel" (if value then Some (createObj !![]) else None)
        /// Enable time traveling via the useTimeTravel hook in children.
        static member inline timeTravel (properties: ITimeTravelProperty list) = Interop.mkRootAttr "timeTravel" (createObj !!properties)

    [<Erase>]
    type timeTravel =
        /// Sets the max history buffer.
        static member inline maxHistory (value: int) = Interop.mkTimeTravelAttr "maxHistory" value

    type Recoil with
        /// Provides the context in which atoms have values. 
        /// 
        /// Must be an ancestor of any component that uses any Recoil hooks. 
        /// 
        /// Multiple roots may co-exist; atoms will have distinct values 
        /// within each root. If they are nested, the innermost root will 
        /// completely mask any outer roots.
        static member inline root (children: ReactElement list) =
            Bindings.Recoil.RecoilRoot(createObj [
                "children" ==> Interop.reactApi.Children.toArray(children)
            ])
        /// Provides the context in which atoms have values. 
        /// 
        /// Must be an ancestor of any component that uses any Recoil hooks. 
        /// 
        /// Multiple roots may co-exist; atoms will have distinct values 
        /// within each root. If they are nested, the innermost root will 
        /// completely mask any outer roots.
        static member inline root (props: IRootProperty list) =
            let props = unbox<RootProps>(createObj !!props)

            Bindings.Recoil.RecoilRoot(createObj [
                "initializeState" ==> (fun o -> 
                    if props.initializer.IsSome then
                        props.initializer.Value o
                    if props.useLocalStorage.IsSome then
                        Storage.Hydrator(o, localStorage) |> props.useLocalStorage.Value
                    if props.useSessionStorage.IsSome then
                        Storage.Hydrator(o, sessionStorage) |> props.useSessionStorage.Value
                )
                "children" ==> (
                    match props.logging, props.useLocalStorage.IsSome with
                    | Some true, true -> [ Storage.observer(); Logger.logger() ] @ props.children
                    | Some true, false -> Logger.logger()::props.children
                    | None, true -> Storage.observer()::props.children
                    | _ -> props.children
                    |> fun children ->
                        match props.timeTravel with
                        | Some props ->
                            TimeTravel.rootWrapper {| otherChildren = children; maxHistory = props.maxHistory |}
                            |> Interop.reactApi.Children.toArray
                        | _ -> Interop.reactApi.Children.toArray(children)
                )
            ])
