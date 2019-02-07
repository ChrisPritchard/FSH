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
        "echo", (echo, "prints out all text following the echo command to output")
        "dir", (dir, "same as ls, will list all files and directories. arguments are [path] [searchPattern], both optional")
        "ls", (dir, "same as dir, will list all files and directories. arguments are [path] [searchPattern], both optional")
        "cd", (cd, "changes the current directory to the directory specified by the first argument")
        "clear", (clear, "clears the console")
    ] |> Map.ofList<string, (string list -> unit) * string>

let help args = 
    if List.isEmpty args then
        printfn ""
        printfn "The following builtin commands are supported by FSH:"
        printfn ""
        builtins |> Map.toList |> List.sortBy fst |> List.iter (fun (n, _) -> printfn "\t%s" n)
        printfn ""
        printfn "For further info on a command, use help [command name] [command name2] etc, e.g. 'help echo'"
        printfn ""
    else
        args 
        |> List.choose (fun a -> 
            Map.tryFind a builtins |> Option.bind (fun d -> Some (a, d)))
        |> List.iter (fun (n, (_, s)) -> 
            printfn "%s: %s" n s)