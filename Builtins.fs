module Builtins

open System.IO

let echo args path = 
    printfn "%s" (String.concat " " args)
    path

let dir args path = 
    let searchPath = if List.length args = 0 then path else Path.Combine (path, args.[0])
    let searchPattern = if List.length args <= 1 then "*" else args.[1]
    Directory.GetDirectories (searchPath, searchPattern) |> Seq.iter (Path.GetFileName >> printfn "%s/")
    Directory.GetFiles (searchPath, searchPattern) |> Seq.iter (Path.GetFileName >> printfn "%s")
    path

let builtins = 
    [
        "echo", echo
        "dir", dir
        "ls", dir
    ] |> Map.ofList<string, string list -> string -> string>