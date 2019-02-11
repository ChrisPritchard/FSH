/// Contains the readLine method, and private support methods, which reimplement Console.ReadLine with support for tab completion.
/// Tab completion attempts to finish the users current token from files, directories and the builtin list.
/// As tab completion requires we intercept the readkey, we therefore need to implement
/// the rest of the console readline functionality, like arrows and backspace.
module LineReader

open System
open System.IO
open Builtins
open Terminal

let private common candidates =
    ""

let private attemptTabCompletion soFar pos = 
    let last = parts soFar |> List.last
    let directory = Path.GetDirectoryName (Path.Combine(currentDir (), last))

    let files = Directory.GetFiles directory |> Array.map Path.GetFileName |> Array.toList
    let folders = Directory.GetDirectories directory |> Array.map Path.GetFileName |> Array.toList
    let allOptions = files @ folders @ List.map fst builtins

    let candidates = allOptions |> List.filter (fun n -> n.ToLower().StartsWith(last.ToLower()))

    if candidates.Length = 0 then 
        soFar, pos // no change
    elif candidates.Length = 1 then
        let matched = if candidates.Length = 1 then candidates.[0] else common candidates
        let newPart = matched.Substring last.Length
        (soFar + newPart), (pos + newPart.Length)
    else
        soFar, pos

let rec private reader start (soFar: string) pos =
    // By printing out the current content of the line after every char
    // implementing backspace and delete becomes easier.
    cursor false
    Console.CursorLeft <- start
    printf "%s%s" soFar (new String(' ', Console.WindowWidth - start - soFar.Length - 1))
    Console.CursorLeft <- start + pos
    cursor true

    let next = Console.ReadKey(true)

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