/// All builtins are defined and aggregated here.
/// A builtin is a custom command that provides a shell function, e.g. cd which changes the shells current directory.
/// The builtins are each a custom function that takes input arguments.
/// The final 'builtin' list exposes these functions and the command that invokes them (usually the same, but there are synonyms like ? for help) 
/// to the processCommand function in Program.fs
module Builtins

open System
open System.IO
open System.Collections

/// Returns the current process directory. By default this is where it was started, and can be changed with the cd builtin.
let currentDir () = Directory.GetCurrentDirectory ()

/// Clears the console window.
let private clear _ _ _ = 
    Console.Clear ()

/// Reads out the arguments passed into the output.
let private echo args writeOut _ = 
    writeOut (sprintf "%s" (String.concat " " args))

/// Lists the contents of the current directory.
let private dir args writeOut _ = 
    let searchPath = Path.Combine(currentDir (), if List.isEmpty args then "" else args.[0])
    let searchPattern = if List.length args < 2 then "*" else args.[1]

    if File.Exists searchPath then 
        writeOut (sprintf "%s" (Path.GetFileName searchPath))
    else
        let finalPath, finalPattern = 
            if Directory.Exists searchPath then searchPath, searchPattern
            else if searchPattern = "*" then Path.GetDirectoryName searchPath, Path.GetFileName searchPath
            else Path.GetDirectoryName searchPath, searchPattern
        
        Directory.GetDirectories (finalPath, finalPattern) 
        |> Seq.map (Path.GetFileName >> sprintf "%s/")
        |> Seq.iter writeOut
        
        Directory.GetFiles (finalPath, finalPattern) 
        |> Seq.map (Path.GetFileName >> sprintf "%s")
        |> Seq.iter writeOut

/// Changes the curent process directory.
let private cd args _ writeError =
    if List.isEmpty args then writeError "no path specified"
    else
        let newPath = Path.Combine (currentDir (), args.[0])
        let newPath = if newPath.EndsWith "/" then newPath else newPath + "/"
        if Directory.Exists newPath then 
            Directory.SetCurrentDirectory(newPath)
        else
            writeError "directory not found"
  
/// Creates a new empty directory.
let private mkdir args writeOut writeError =
    if List.isEmpty args then
        writeError "no directory name speciifed"
    else
        let path = Path.Combine (currentDir(), args.[0])
        if Directory.Exists path then
            writeError "directory already exists"
        else
            Directory.CreateDirectory path |> ignore
            writeOut "directory created"
 
/// Deletes a directory, if empty.
let private rmdir args writeOut writeError =
    if List.isEmpty args then
        writeError "no directory name speciifed"
    else
        let path = Path.Combine (currentDir(), args.[0])
        if not (Directory.Exists path) then
            writeError "directory does not exist"
        elif Directory.GetFiles (path, "*", SearchOption.AllDirectories) |> Array.isEmpty |> not then
            writeError "directory was not empty"
        else
            Directory.Delete path |> ignore
            writeOut "directory deleted"

/// Reads out the content of a file to the output.
let private cat args writeOut writeError = 
    if List.isEmpty args then
        writeError "no file specified"
    elif not (File.Exists args.[0]) then
        writeError "file not found"
    else
        writeOut (File.ReadAllText args.[0])

/// Copies a file into a new location.
let private cp args writeOut writeError = 
    if List.length args <> 2 then
        writeError "wrong number of arguments: please specify source and dest"
    else
        let source = Path.Combine(currentDir(), args.[0])
        if not (File.Exists source) then
            writeError "source file path does not exist or is invalid"
        else
            let dest = Path.Combine(currentDir(), args.[1])
            let isDir = Directory.Exists dest
            let baseDir = Path.GetDirectoryName dest
            if not isDir && not (Directory.Exists baseDir) then
                writeError "destination directory or file path does not exist or is invalid"
            elif File.Exists dest then
                writeError "destination file already exists"
            elif not isDir then
                File.Copy (source, dest)
                writeOut "file copied"
            else
                let fileName = Path.GetFileName source
                let dest = Path.Combine(dest, fileName)
                File.Copy (source, dest)
                writeOut "file copied"

/// Moves a file to a new location.
let private mv args writeOut writeError = 
    if List.length args <> 2 then
        writeError "wrong number of arguments: please specify source and dest"
    else
        let source = Path.Combine(currentDir(), args.[0])
        if not (File.Exists source) then
            writeError "source file path does not exist or is invalid"
        else
            let dest = Path.Combine(currentDir(), args.[1])
            let isDir = Directory.Exists dest
            let baseDir = Path.GetDirectoryName dest
            if not isDir && not (Directory.Exists baseDir) then
                writeError "destination directory or file path does not exist or is invalid"
            elif File.Exists dest then
                writeError "destination file already exists"
            elif not isDir then
                File.Move (source, dest)
                writeOut "file moved"
            else
                let fileName = Path.GetFileName source
                let dest = Path.Combine(dest, fileName)
                File.Move (source, dest)
                writeOut "file moved"     

/// Removes a file or directory.             
let private rm args writeOut writeError =
    if List.isEmpty args then
        writeError "no target specified"
    else
        let target = Path.Combine(currentDir(), args.[0])
        if File.Exists target then
            File.Delete target
            writeError "file deleted"
        else if Directory.Exists target then
            if not (Array.isEmpty (Directory.GetFiles target)) then
                writeError "directory is not empty"
            else
                Directory.Delete target
                writeOut "directory deleted"
        else
            writeError "file or directory does not exist"

/// Enumerates all environment variables, or reads/sets a single value.
let private env args writeOut _ = 
    if List.isEmpty args then
        Environment.GetEnvironmentVariables ()
        |> Seq.cast<DictionaryEntry>
        |> Seq.map (fun d -> sprintf "%s=%s" (unbox<string> d.Key) (unbox<string> d.Value))
        |> Seq.iter writeOut
    else if args.Length = 1 then
        writeOut (Environment.GetEnvironmentVariable args.[0])
    else
        Environment.SetEnvironmentVariable (args.[0], args.[1])

/// This list maps the text entered by the user to the implementation to be run, and the help text for the command.
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
        "env", (env, "either lists all env vars, reads a specific var, or sets a var to a value")
        // The following three special builtins are here so that help can access their content.
        // However they have no implementation, as they are invoked by the coreloop and processCommand methods 
        // in Program.fs rather than via the normal builtin execution process.
        "?", ((fun _ _ _ -> ()), "same as help, prints the builtin list, or the help of specific commands")
        "help", ((fun _ _ _ -> ()), "same as ?, prints the builtin list, or the help of specific commands")
        "exit", ((fun _ _ _ -> ()), "exits FSH")
    ] 
/// The map is specifically used to match entered text to implementation.
let builtinMap = 
    builtins 
    |> List.map (fun (commandName, (implementation, _)) -> commandName, implementation) 
    |> Map.ofList

// For the above code, the reason why the map and list is seperate is that a map is ordered by its keys.
// For help, the order of builtin's is important and is preserved in the list.

/// Help provides helptext for a given builtin (including itself).
/// It is defined after the builtin map, as it needs to read from the map to perform its function.
let help args writeOut _ = 
    [
        if List.isEmpty args then
            yield sprintf "\nThe following builtin commands are supported by FSH:\n"
            yield! builtins |> List.map (fun (n, _) -> sprintf "\t%s" n)
            yield sprintf "\nFor further info on a command, use help [command name] [command name2] etc, e.g. 'help echo'\n"
        else
            yield! 
                args 
                |> List.choose (fun commandName -> 
                    List.tryFind (fun item -> fst item = commandName) builtins 
                    |> Option.bind (fun (_, (_, helpText)) -> Some (commandName, helpText)))
                |> List.map (fun result -> 
                    result ||> sprintf "%s: %s")
    ] |> Seq.iter writeOut