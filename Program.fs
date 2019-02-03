open Terminal

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

    let rec endlessEcho () =
        let entered = prompt ()
        if entered = "exit" then ()
        else
            colour "yellow"
            if entered <> "" then printfn "%s" entered
            endlessEcho ()

    endlessEcho ()

    0
