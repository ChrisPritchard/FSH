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

let mkdir args =
    if List.isEmpty args then
        printfn "no directory name speciifed"
    else
        let path = Path.Combine (currentDir(), args.[0])
        if Directory.Exists path then
            printfn "directory already exists"
        else
            Directory.CreateDirectory path |> ignore
 
let rmdir args =
    if List.isEmpty args then
        printfn "no directory name speciifed"
    else
        let path = Path.Combine (currentDir(), args.[0])
        if not (Directory.Exists path) then
            printfn "directory does not exist"
        elif Directory.GetFiles (path, "*", SearchOption.AllDirectories) |> Array.isEmpty |> not then
            printfn "directory was not empty"
        else
            Directory.Delete path |> ignore

let cat args = 
    if List.isEmpty args then
        printfn "no file specified"
    elif not (File.Exists args.[0]) then
        printfn "file not found"
    else
        printfn "%s" (File.ReadAllText args.[0])

let builtins = 
    [
        "echo", (echo, "prints out all text following the echo command to output")
        "dir", (dir, "same as ls, will list all files and directories. arguments are [path] [searchPattern], both optional")
        "ls", (dir, "same as dir, will list all files and directories. arguments are [path] [searchPattern], both optional")
        "cd", (cd, "changes the current directory to the directory specified by the first argument")
        "clear", (clear, "clears the console")
        "mkdir", (mkdir, "creates a new directory at the position specified by the first argument")
        "rmdir", (rmdir, "removes an empty directory at the position specified by the first argument")
        "?", ((fun _ -> ()), "same as help, prints this page, or the help of specific commands")
        "help", ((fun _ -> ()), "same as ?, prints this page, or the help of specific commands")
        "exit", ((fun _ -> ()), "exits FSH")
        "cat", (cat, "prints the contents of the file specified to the output")
    ] |> Map.ofList<string, (string list -> unit) * string>

let help args = 
    if List.isEmpty args then
        printfn "\nThe following builtin commands are supported by FSH:\n"
        builtins |> Map.toList |> List.sortBy fst |> List.iter (fun (n, _) -> printfn "\t%s" n)
        printfn "\nFor further info on a command, use help [command name] [command name2] etc, e.g. 'help echo'\n"
    else
        args 
        |> List.choose (fun a -> 
            Map.tryFind a builtins |> Option.bind (fun d -> Some (a, d)))
        |> List.iter (fun (n, (_, s)) -> 
            printfn "%s: %s" n s)