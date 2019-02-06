module Builtins

let echo args path = 
    printfn "%s" (String.concat " " args)
    path

let builtins = 
    [
        "echo", echo        
    ] |> Map.ofList<string, seq<string> -> string -> string>