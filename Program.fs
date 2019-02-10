open Terminal
open Builtins

open System.Diagnostics
open System.ComponentModel

[<EntryPoint>]
let main _ =

    cursor false

    defaultColour ()
    printfn "For a list of commands type '?' or 'help'"

    let prompt () = 
        colour "Magenta"
        printf "FSH %s> " (currentDir ())
        cursor true
        defaultColour ()
        let read = readLine ()
        cursor false
        read
   
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

            if command = "help" || command = "?" then
                help parts.[1..]
            else
                match Map.tryFind command builtins with
                | Some (f, _) -> 
                    f parts.[1..]
                | None ->
                    launchProcess command parts.[1..]

    let rec coreLoop () =
        let entered = prompt ()
        if entered.Trim() = "exit" then ()
        else
            processCommand entered
            coreLoop ()

    coreLoop ()

    0
