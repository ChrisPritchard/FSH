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
    let startPos = Console.CursorLeft
    let startLine = Console.CursorTop

    /// Creates a string of whitespace, n characters long
    let whitespace n = new String(' ', n)

    /// This will render a given line aligned to the prompt
    /// It will also print whitespace for the rest of the line, in order to 'overwrite' any existing printed text
    let linePrinter isFirst isLast line = 
        sprintf "%s%s%s%s"
            (if isFirst then "" else whitespace startPos)
            line 
            (whitespace (Console.WindowWidth - startPos - line.Length - 1))
            (if not isLast then "\r\n" else "")
    
    /// When using back and forth in history (up down arrows), this function is used to break up the history string into lines and last line
    let asLines (prior: string) = 
        let lines = 
            prior.Split([|"\r\n"|], StringSplitOptions.RemoveEmptyEntries)
            |> Array.toList
        List.rev lines.[0..lines.Length-2], lines.[lines.Length-1]

    // This recursively prompts for input from the user, producing a final string result on the reception of the Enter key.
    let rec reader priorIndex lines (soFar: string) pos =

        // By printing out the current content of the line after every char
        // implementing backspace and delete becomes easier.
        cursor false
        Console.CursorLeft <- startPos
        Console.CursorTop <- startLine
        soFar::lines 
        |> List.rev |> List.mapi (fun i -> linePrinter (i = 0) (i = lines.Length))
        |> String.concat "" |> parts |> tokens |> writeTokens
        Console.CursorLeft <- startPos + pos
        cursor true
        
        // blocks here until a key is read
        let next = Console.ReadKey true

        match next.Key with
        | ConsoleKey.Enter when next.Modifiers <> ConsoleModifiers.Shift ->
            printfn "" // write a final newline
            (soFar::lines) |> List.rev |> String.concat "\r\n"
        | ConsoleKey.Enter ->
            reader priorIndex (soFar::lines) "" 0
        | ConsoleKey.Backspace when Console.CursorLeft <> startPos ->
            let nextSoFar = soFar.[0..pos-2] + soFar.[pos..]
            let nextPos = max 0 (pos - 1)
            reader priorIndex lines nextSoFar nextPos
        | ConsoleKey.Delete ->
            let nextSoFar = soFar.[0..pos-1] + soFar.[pos+1..]
            reader priorIndex lines nextSoFar pos
        | ConsoleKey.LeftArrow ->
            let nextPos = max 0 (pos - 1)
            reader priorIndex lines soFar nextPos
        | ConsoleKey.RightArrow ->
            let nextPos = min soFar.Length (pos + 1)
            reader priorIndex lines soFar nextPos
        | ConsoleKey.UpArrow when priorIndex < List.length prior - 1 ->
            let nextIndex = priorIndex + 1
            let nextLines, nextSoFar = asLines prior.[nextIndex]
            let nextPos = nextSoFar.Length
            reader nextIndex nextLines nextSoFar nextPos
        | ConsoleKey.DownArrow when priorIndex > 0 ->
            let nextIndex = priorIndex - 1
            let nextLines, nextSoFar = asLines prior.[nextIndex]
            let nextPos = nextSoFar.Length
            reader nextIndex nextLines nextSoFar nextPos
        | ConsoleKey.Home ->
            reader priorIndex lines soFar 0
        | ConsoleKey.End ->
            reader priorIndex lines soFar soFar.Length
        | ConsoleKey.Tab when soFar <> "" ->
            let (soFar, pos) = attemptTabCompletion soFar pos
            reader priorIndex lines soFar pos
        | _ ->
            let c = next.KeyChar
            if not (Char.IsControl c) then
                reader priorIndex lines (soFar.[0..pos-1] + string c + soFar.[pos..]) (pos + 1)
            else
                reader priorIndex lines soFar pos

    reader -1 [] "" 0