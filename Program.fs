open Terminal

open System
open System.Diagnostics
open System.ComponentModel

[<EntryPoint>]
let main _ =

    cursor false

    defaultColour ()
    printfn "For commands type '?', 'man' 'help'"

    let prompt path = 
        colour "Magenta"
        printf "FSH %s> " path
        cursor true
        defaultColour ()
        let read = readLine ()
        cursor false
        read

    let parts s = 
        let (last, parts, _) =
            ((None, [], ' '), s) 
            ||> Seq.fold (fun (current, soFar, last) c ->
                    let next = 
                        match c, current with
                        | '\"', None when last = '\\' -> Some "\""
                        | '\"', Some n when last = '\\' -> Some (n + "\"")
                        | '\"', None -> Some ""
                        | '\"', Some _ -> None
                        | ' ', Some _ -> None
                        | c, Some n -> Some (n + string c)
                        | c, None -> Some (string c)
                    let soFar = 
                        match next, current with
                        | None, Some n -> n::soFar
                        | _ -> soFar
                    (next, soFar, c))
        let final = match last with None -> parts | Some s -> s::parts
        List.rev final
    
    let launchProcess (s: string) =
        let fileName, arguments = 
            match Seq.tryFindIndex ((=) ' ') s with None -> s, "" | Some i -> s.[0..i-1], s.[i..]
            
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

    let processCommand path (s : string) =
        let test = parts s
        if s.Length = 0 then path // no command so just loop
        else if s.[0] = '(' then path // start fsi
        else
            launchProcess s
            path

    let rec coreLoop path =
        let entered = prompt path
        if entered = "exit" then ()
        else
            let nextPath = processCommand path entered
            coreLoop nextPath

    coreLoop AppDomain.CurrentDomain.BaseDirectory

    0
