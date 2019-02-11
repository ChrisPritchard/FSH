/// Contains the readLine method, and private support methods, which reimplement Console.ReadLine with support for tab completion.
/// Tab completion attempts to finish the users current token from files, directories and the builtin list.
/// As tab completion requires we intercept the readkey, we therefore need to implement
/// the rest of the console readline functionality, like arrows and backspace.
module LineReader

open System
open System.IO
open Builtins
open Terminal

/// For a set of strings, will return the common start string.
let private common startIndex (candidates : string list) =
    // finds the first index that is longer than a candidate or for which two or more candidates differ
    let uncommonPoint = 
        [startIndex..Console.WindowWidth]
        |> List.find (fun i ->
            if i >= candidates.[0].Length then true
            else 
                let charAt = candidates.[0].[i]
                List.tryFind (fun (c : string) -> 
                    c.Length = i || c.[i] <> charAt) candidates <> None)
    // return the component prior to this found index
    candidates.[0].[0..uncommonPoint-1]
    
/// Attempts to find the closest matching file, directory or builtin for the final path token.
/// If multiple candidates are found, only the common content is returned.
let private attemptTabCompletion soFar pos = 
    let last = parts soFar |> List.last
    let asPath = Path.Combine(currentDir (), last)
    let directory = Path.GetDirectoryName asPath

    let files = Directory.GetFiles directory |> Array.map Path.GetFileName |> Array.toList
    let folders = Directory.GetDirectories directory |> Array.map Path.GetFileName |> Array.toList
    let allOptions = files @ folders @ List.map fst builtins

    let candidates = allOptions |> List.filter (fun n -> n.ToLower().StartsWith(last.ToLower()))

    if candidates.Length = 0 then 
        soFar, pos // no change
    else
        let matched = if candidates.Length = 1 then candidates.[0] else common last.Length candidates
        let soFar = soFar.[0..pos-last.Length-1] + matched
        soFar, soFar.Length

/// This recursively prompts for input from the user, producing a final string result on the reception of the Enter key.
let rec private reader start (soFar: string) pos =
    // By printing out the current content of the line after every char
    // implementing backspace and delete becomes easier.
    cursor false
    Console.CursorLeft <- start
    printf "%s%s" soFar (new String(' ', Console.WindowWidth - start - soFar.Length - 1))
    Console.CursorLeft <- start + pos
    cursor true

    // blocks here until a key is read
    let next = Console.ReadKey true

    if next.Key = ConsoleKey.Enter then
        printfn "" // write a final newline
        soFar
    elif next.Key = ConsoleKey.Backspace && Console.CursorLeft <> start then
        reader start (soFar.[0..pos-2] + soFar.[pos..]) (max 0 (pos - 1))
    elif next.Key = ConsoleKey.Delete then 
        reader start (soFar.[0..pos-1] + soFar.[pos+1..]) pos
    elif next.Key = ConsoleKey.LeftArrow then
        reader start soFar (max 0 (pos - 1))
    elif next.Key = ConsoleKey.RightArrow then
        reader start soFar (min soFar.Length (pos + 1))
    elif next.Key = ConsoleKey.Home then
        reader start soFar 0
    elif next.Key = ConsoleKey.End then
        reader start soFar soFar.Length
    elif next.Key = ConsoleKey.Tab && soFar <> "" then 
        let (soFar, pos) = attemptTabCompletion soFar pos
        reader start soFar pos
    else
        let c = next.KeyChar
        if not (Char.IsControl c) then
            reader start (soFar.[0..pos-1] + string c + soFar.[pos..]) (pos + 1)
        else
            reader start soFar pos

/// Reads a line of input from the user, enhanced for automatic tabbing and the like.
let readLine () = 
    let start = Console.CursorLeft
    reader start "" 0