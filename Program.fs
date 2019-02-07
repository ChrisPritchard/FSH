open Terminal
open Builtins

open System.Diagnostics
open System.ComponentModel

[<EntryPoint>]
let main _ =

    cursor false

    defaultColour ()
    printfn "For commands type '?', 'man' 'help'"

    let prompt () = 
        colour "Magenta"
        printf "FSH %s> " (currentDir ())
        cursor true
        defaultColour ()
        let read = readLine ()
        cursor false
        read

    let parts s =
        let rec parts soFar quoted last remainder = 
            [
                if remainder = "" then yield soFar
                else
                    let c, next = remainder.[0], remainder.[1..]
                    match c with
                    | '\"' when soFar = "" -> 
                        yield! parts soFar true last next
                    | '\"' when last <> '\\' && quoted ->
                        yield soFar
                        yield! parts "" false last next
                    | ' ' when last <> '\\' && not quoted ->
                        if soFar <> "" then yield soFar
                        yield! parts "" quoted last next
                    | _ -> 
                        yield! parts (soFar + string c) quoted c next
            ]
        parts "" false ' ' s
    
    let launchProcess fileName args =
        let op = 
            new ProcessStartInfo(fileName, args |> List.map (sprintf "\"%s\"") |> String.concat " ",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false)
            |> fun i -> new Process (StartInfo = i)
                
        op.OutputDataReceived.Add(fun e -> printfn "%s" e.Data)
        op.ErrorDataReceived.Add(fun e -> printfn "%s" e.Data)

        try
            op.Start () |> ignore

            colour "Green"
            op.BeginOutputReadLine ()
            op.WaitForExit ()
            op.CancelOutputRead ()
        with
            | :? Win32Exception as ex -> 
                colour "Red"
                printfn "%s: %s" fileName ex.Message

    let processCommand (s : string) =
        if s.Length = 0 then () // no command so just loop
        else 
            let parts = parts s
            let command = parts.[0]

            match Map.tryFind command builtins with
            | Some f -> 
                f parts.[1..]
            | None ->
                launchProcess command parts.[1..]

    let rec coreLoop () =
        let entered = prompt ()
        if entered = "exit" then ()
        else
            let nextPath = processCommand entered
            coreLoop nextPath

    coreLoop ()

    0
