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

/// Reads a line of input from the user, enhanced for automatic tabbing and the like.
/// Prior is a list of prior input lines, used for history navigation
let readLine (prior: string list) = 
    let start = Console.CursorLeft

    // This recursively prompts for input from the user, producing a final string result on the reception of the Enter key.
    let rec reader priorIndex (soFar: string) pos =

        // By printing out the current content of the line after every char
        // implementing backspace and delete becomes easier.
        cursor false
        Console.CursorLeft <- start
        printf "%s%s" soFar (new String(' ', Console.WindowWidth - start - soFar.Length - 1))
        Console.CursorLeft <- start + pos
        cursor true

        // blocks here until a key is read
        let next = Console.ReadKey true

        match next.Key with
        | ConsoleKey.Enter ->
            printfn "" // write a final newline
            soFar
        | ConsoleKey.Backspace when Console.CursorLeft <> start ->
            let nextSoFar = soFar.[0..pos-2] + soFar.[pos..]
            let nextPos = max 0 (pos - 1)
            reader priorIndex nextSoFar nextPos
        | ConsoleKey.Delete ->
            let nextSoFar = soFar.[0..pos-1] + soFar.[pos+1..]
            reader priorIndex nextSoFar pos
        | ConsoleKey.LeftArrow ->
            let nextPos = max 0 (pos - 1)
            reader priorIndex soFar nextPos
        | ConsoleKey.RightArrow ->
            let nextPos = min soFar.Length (pos + 1)
            reader priorIndex soFar nextPos
        | ConsoleKey.UpArrow when priorIndex < List.length prior - 1 ->
            let nextIndex = priorIndex + 1
            let nextSoFar = prior.[nextIndex]
            let nextPos = nextSoFar.Length
            reader nextIndex nextSoFar nextPos
        | ConsoleKey.DownArrow when priorIndex > 0 ->
            let nextIndex = priorIndex - 1
            let nextSoFar = prior.[nextIndex]
            let nextPos = nextSoFar.Length
            reader nextIndex nextSoFar nextPos
        | ConsoleKey.Home ->
            reader priorIndex soFar 0
        | ConsoleKey.End ->
            reader priorIndex soFar soFar.Length
        | ConsoleKey.Tab when soFar <> "" ->
            let (soFar, pos) = attemptTabCompletion soFar pos
            reader priorIndex soFar pos
        | _ ->
            let c = next.KeyChar
            if not (Char.IsControl c) then
                reader priorIndex (soFar.[0..pos-1] + string c + soFar.[pos..]) (pos + 1)
            else
                reader priorIndex soFar pos

    reader -1 "" 0