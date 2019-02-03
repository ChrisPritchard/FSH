open Terminal

[<EntryPoint>]
let main _ =
    colour "Green"

    writeLine (sprintf "Welcome to - SYMENET -")

    defaultColour ()

    writeLine ""
    writeLine ""
    writeLine "If you don't know what to type next, try '?' or 'help'"

    let prompt () = 
        colour "Red"
        write ".\> "
        defaultColour ()
        readLine ()

    let rec endlessEcho () =
        let entered = prompt ()
        colour "yellow"
        if entered <> "" then writeLine entered
        endlessEcho ()

    endlessEcho ()

    0
