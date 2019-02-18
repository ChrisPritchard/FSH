/// All builtins are defined and aggregated here.
/// A builtin is a custom command that provides a shell function, e.g. cd which changes the shells current directory.
/// The builtins are each a custom function that takes input arguments.
/// The final 'builtin' list exposes these functions and the command that invokes them (usually the same, but there are synonyms like ? for help) 
/// to the processCommand function in Program.fs
module Builtins

open System
open System.IO

/// Returns the current process directory. By default this is where it was started, and can be changed with the cd builtin.
let currentDir () = Directory.GetCurrentDirectory ()

let clear _ = 
    Console.Clear ()

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
            
let mkdir args =
    if List.isEmpty args then
        printfn "no directory name speciifed"
    else
        let path = Path.Combine (currentDir(), args.[0])
        if Directory.Exists path then
            printfn "directory already exists"
        else
            Directory.CreateDirectory path |> ignore
            printfn "directory created"
 
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
            printfn "directory deleted"

let cat args = 
    if List.isEmpty args then
        printfn "no file specified"
    elif not (File.Exists args.[0]) then
        printfn "file not found"
    else
        printfn "%s" (File.ReadAllText args.[0])

let cp args = 
    if List.length args <> 2 then
        printfn "wrong number of arguments: please specify source and dest"
    else
        let source = Path.Combine(currentDir(), args.[0])
        if not (File.Exists source) then
            printfn "source file path does not exist or is invalid"
        else
            let dest = Path.Combine(currentDir(), args.[1])
            let isDir = Directory.Exists dest
            let baseDir = Path.GetDirectoryName dest
            if not isDir && not (Directory.Exists baseDir) then
                printfn "destination directory or file path does not exist or is invalid"
            elif File.Exists dest then
                printfn "destination file already exists"
            elif not isDir then
                File.Copy (source, dest)
                printfn "file copied"
            else
                let fileName = Path.GetFileName source
                let dest = Path.Combine(dest, fileName)
                File.Copy (source, dest)
                printfn "file copied"

let mv args = 
    if List.length args <> 2 then
        printfn "wrong number of arguments: please specify source and dest"
    else
        let source = Path.Combine(currentDir(), args.[0])
        if not (File.Exists source) then
            printfn "source file path does not exist or is invalid"
        else
            let dest = Path.Combine(currentDir(), args.[1])
            let isDir = Directory.Exists dest
            let baseDir = Path.GetDirectoryName dest
            if not isDir && not (Directory.Exists baseDir) then
                printfn "destination directory or file path does not exist or is invalid"
            elif File.Exists dest then
                printfn "destination file already exists"
            elif not isDir then
                File.Move (source, dest)
                printfn "file moved"
            else
                let fileName = Path.GetFileName source
                let dest = Path.Combine(dest, fileName)
                File.Move (source, dest)
                printfn "file moved"     
                
let rm args =
    if List.isEmpty args then
        printfn "no target specified"
    else
        let target = Path.Combine(currentDir(), args.[0])
        if File.Exists target then
            File.Delete target
            printfn "file deleted"
        else if Directory.Exists target then
            if not (Array.isEmpty (Directory.GetFiles target)) then
                printf "directory is not empty"
            else
                Directory.Delete target
                printfn "directory deleted"
        else
            printfn "file or directory does not exist"

let builtins = 
    [
        "clear", (clear, "clears the console")
        "echo", (echo, "prints out all text following the echo command to output")
        "dir", (dir, "same as ls, will list all files and directories. arguments are [path] [searchPattern], both optional")
        "ls", (dir, "same as dir, will list all files and directories. arguments are [path] [searchPattern], both optional")
        "cd", (cd, "changes the current directory to the directory specified by the first argument")
        "mkdir", (mkdir, "creates a new directory at the position specified by the first argument")
        "rmdir", (rmdir, "removes an empty directory at the position specified by the first argument")
        "cat", (cat, "prints the contents of the file specified to the output")
        "cp", (cp, "copies the source file to the destination folder or filepath")
        "mv", (mv, "moves the source file to the destination folder or filepath")
        "rm", (rm, "same as del, deletes the target file or empty directory")
        "del", (rm, "same as rm, deletes the target file or empty directory")
        // The following three special builtins are here so that help can access their content.
        // However they have no implementation, as they are invoked by the coreloop and processCommand methods 
        // in Program.fs rather than via the normal builtin execution process.
        "?", ((fun _ -> ()), "same as help, prints the builtin list, or the help of specific commands")
        "help", ((fun _ -> ()), "same as ?, prints the builtin list, or the help of specific commands")
        "exit", ((fun _ -> ()), "exits FSH")
    ] 
let builtinMap = 
    builtins 
    |> List.map (fun (commandName, (implementation, _)) -> commandName, implementation) 
    |> Map.ofList

/// Help provides helptext for a given builtin (including itself).
/// It is defined after the builtin map, as it needs to read from the map to perform its function.
let help args = 
    if List.isEmpty args then
        printfn "\nThe following builtin commands are supported by FSH:\n"
        builtins |> List.iter (fun (n, _) -> printfn "\t%s" n)
        printfn "\nFor further info on a command, use help [command name] [command name2] etc, e.g. 'help echo'\n"
    else
        args 
        |> List.choose (fun commandName -> 
            List.tryFind (fun item -> fst item = commandName) builtins 
            |> Option.bind (fun (_, (_, helpText)) -> Some (commandName, helpText)))
        |> List.iter (fun result -> 
            result ||> printfn "%s: %s")