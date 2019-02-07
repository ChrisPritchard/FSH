module Builtins

open System.IO

let echo args path = 
    printfn "%s" (String.concat " " args)
    path

let dir args path = 
    let searchPath = Path.Combine(path, if List.length args = 0 then "" else args.[0])
    let searchPattern = if List.length args < 2 then "*" else args.[1]

    if File.Exists searchPath then 
        printfn "%s" (Path.GetFileName searchPath)
        path
    else
        let finalPath, finalPattern = 
            if Directory.Exists searchPath then searchPath, searchPattern
            else if searchPattern = "*" then Path.GetDirectoryName searchPath, Path.GetFileName searchPath
            else Path.GetDirectoryName searchPath, searchPattern
        
        Directory.GetDirectories (finalPath, finalPattern) 
            |> Seq.iter (Path.GetFileName >> printfn "%s/")
        Directory.GetFiles (finalPath, finalPattern) 
            |> Seq.iter (Path.GetFileName >> printfn "%s")
        path

let builtins = 
    [
        "echo", echo
        "dir", dir
        "ls", dir
    ] |> Map.ofList<string, string list -> string -> string>