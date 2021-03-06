﻿namespace Feliz.Recoil

open Fable.Core
open Feliz
open System.ComponentModel

[<EditorBrowsable(EditorBrowsableState.Never);RequireQualifiedAccess>]
module Logger =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    let logAction (name: string, atomValue: 'T, prevAtomValue: 'T option) =
        JS.console.groupCollapsed(
            sprintf "[%s]: Atom - %s" 
                (System.DateTime.Now.ToLongTimeString())
                name
        )

        JS.console.group("Current")
        JS.console.log(atomValue)
        JS.console.groupEnd()

        JS.console.group("Previous")
        match prevAtomValue with
        | Some v -> JS.console.log(v)
        | None -> JS.console.log("No previous value.")
        JS.console.groupEnd()

        JS.console.groupEnd()
    
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    let logger = React.functionComponent(fun () ->
        #if DEBUG
        Recoil.useTransactionObservation <| fun o ->
            o.modifiedAtoms 
            |> Set.iter (fun (name, _) ->
                o.atomInfo.TryFind(name)
                |> Option.iter (fun atomInfo -> 
                    // Until https://github.com/facebookexperimental/Recoil/issues/277 is resolved
                    // we just log any flagged atom
                    atomInfo.persistence.type'
                    |> Option.iter (fun _ ->
                        o.atomValues.TryFind(name)
                        |> Option.iter(fun value ->
                            logAction (
                                name,
                                value,
                                o.previousAtomValues.TryFind(name)
                            )
                        )
                    )
                )
            )
        #endif

        Html.none)
