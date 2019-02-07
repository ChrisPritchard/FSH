module Builtins

open System.IO
open Terminal
open System

let echo args = 
    printfn "%s" (String.concat " " args)

let dir args = 
    let searchPath = Path.Combine(currentDir (), if List.isEmpty args then "" else args.[0])
    let searchPattern = if List.length args < 2 then "*" else args.[1]

    if File.Exists searchPath then 
        printfn "%s" (Path.GetFileName searchPath)
    else
        let finalPath, finalPattern = 
            if Directory.Exists searchPath then searchPath, searchPattern
            else if searchPattern = "*" then Path.GetDirectoryName searchPath, Path.GetFileName searchPath
            else Path.GetDirectoryName searchPath, searchPattern
        
        Directory.GetDirectories (finalPath, finalPattern) 
            |> Seq.iter (Path.GetFileName >> printfn "%s/")
        Directory.GetFiles (finalPath, finalPattern) 
            |> Seq.iter (Path.GetFileName >> printfn "%s")

let cd args =
    if List.isEmpty args then ()
    else
        let newPath = Path.Combine (currentDir (), args.[0])
        let newPath = if newPath.EndsWith "/" then newPath else newPath + "/"
        if Directory.Exists newPath then 
            Directory.SetCurrentDirectory(newPath)
        else
            printfn "directory not found"

let clear _ = 
    Console.Clear ()

let builtins = 
    [
        "echo", echo
        "dir", dir
        "ls", dir
        "cd", cd
        "clear", clear
    ] |> Map.ofList<string, string list -> unit>