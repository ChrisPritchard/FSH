open Terminal
open System

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
        printf ".\> "
        cursor true
        defaultColour ()
        let read = readLine ()
        cursor false
        read

    let processCommand (s : string) =
        let parts = s.Split ([|" "|], StringSplitOptions.None)
        if parts.Length > 0 then
            if parts.[0] = "echo" then
                colour "yellow"
                printfn "%s" s.[5..]
            else
                printfn "%s: command not found" parts.[0]

    let rec coreLoop () =
        let entered = prompt ()
        if entered = "exit" then ()
        else
            processCommand entered
            coreLoop ()

    coreLoop ()

    0
