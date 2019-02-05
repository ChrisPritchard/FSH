open Terminal

open System
open System.Threading
open System.Diagnostics
open System.IO

[<EntryPoint>]
let main _ =

    cursor false
    colour "Green"

    printfn "Welcome to - SYMENET -"

    defaultColour ()

    printfn ""
    printfn ""
    printfn "If you don't know what to type next, try '?' or 'help'"

    let prompt () = 
        colour "Red"
        printf "fsh[%s]> " (AppDomain.CurrentDomain.BaseDirectory)
        cursor true
        defaultColour ()
        let read = readLine ()
        cursor false
        read

    let processCommand (s : string) =
        if s.Length = 0 then () // no command so just loop
        else if s.[0] = '(' then () // start fsi
        else
            let fileName, arguments = 
                match Seq.tryFindIndex ((=) ' ') s with None -> s, "" | Some i -> s.[0..i-1], s.[i..]
            
            if not (File.Exists fileName) then
                colour "Yellow"
                printfn "%s: executable not found" fileName
            else
                let op = 
                    new ProcessStartInfo(fileName, arguments,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false)
                    |> fun i -> new Process (StartInfo = i)
                
                op.OutputDataReceived.Add(fun e -> printfn "%s" e.Data)
                op.ErrorDataReceived.Add(fun e -> printfn "%s" e.Data)

                op.Start () |> ignore

                colour "Green"
                op.BeginOutputReadLine ()
                op.WaitForExit ()
                op.CancelOutputRead ()

    let rec coreLoop () =
        let entered = prompt ()
        if entered = "exit" then ()
        else
            let nextPath = processCommand entered
            coreLoop nextPath

    coreLoop ()

    0
