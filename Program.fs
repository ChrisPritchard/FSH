open Terminal
open System

type FileSystem = 
    | File of name:string * size:int
    | Folder of name:string * FileSystem list

[<EntryPoint>]
let main _ =

    let root = [
        Folder ("bin", [])
        Folder ("etc", [])
        Folder ("home", [
            Folder ("user", [])
        ])
    ]

    cursor false
    colour "Green"

    printfn "Welcome to - SYMENET -"

    defaultColour ()

    printfn ""
    printfn ""
    printfn "If you don't know what to type next, try '?' or 'help'"

    let prompt path = 
        colour "Red"
        printf "user:%s$ " path
        cursor true
        defaultColour ()
        let read = readLine ()
        cursor false
        read

    let processCommand path (s : string) =
        let parts = s.Split ([|" "|], StringSplitOptions.None)
        if parts.Length = 0 then path
        else
            if parts.[0] = "echo" then
                colour "Yellow"
                printfn "%s" s.[5..]
                path
            else
                printfn "%s: command not found" parts.[0]
                path

    let rec coreLoop path =
        let entered = prompt path
        if entered = "exit" then ()
        else
            let nextPath = processCommand path entered
            coreLoop nextPath

    coreLoop "/home/user"

    0
