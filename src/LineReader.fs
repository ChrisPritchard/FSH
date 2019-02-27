/// Contains the readLine method, and private support methods, which reimplement Console.ReadLine with support for tab completion.
/// Tab completion attempts to finish the users current token from files, directories and the builtin list.
/// As tab completion requires we intercept the readkey, we therefore need to implement
/// the rest of the console readline functionality, like arrows and backspace.
module LineReader

open System
open System.IO
open Constants
open Builtins
open LineParser
open Model

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

/// Writes out a list of tokens to the output, coloured appropriately.
/// This also ensures the print out is aligned with the prompt
let private writeTokens promptPos tokens = 
    let align () = if Console.CursorLeft < promptPos then Console.CursorLeft <- promptPos
    let clearLine () = printf "%s" (new String(' ', Console.WindowWidth - Console.CursorLeft - 1))
    let printAligned (s: string) = 
        let lines = s.Split "\r\n"
        lines |> Array.iteri (fun i line -> 
            align ()
            printf "%s " line 
            if i <> Array.length lines - 1 then 
                clearLine ()
                printfn "")
    tokens 
    |> List.iter (fun token ->
        match token with
        | Command (s, args) -> 
            apply Colours.command
            printAligned s
            apply Colours.argument
            args |> Seq.iter printAligned
        | Code s ->
            apply Colours.code
            printAligned s
        | Pipe ->
            apply Colours.pipe
            printAligned "|>"
        | Out s ->
            apply Colours.pipe
            printAligned ">>"
            apply Colours.argument
            printAligned s
        | Whitespace n ->
            printAligned (new String(' ', n))
        | Linebreak ->
            clearLine ()
            printfn "")
    clearLine ()

/// Reads a line of input from the user, enhanced for automatic tabbing and the like.
/// Prior is a list of prior input lines, used for history navigation
let readLine (prior: string list) = 
    let startPos = Console.CursorLeft
    let startLine = Console.CursorTop

    /// Takes a string (single or multiline) and prints it coloured by type to the output.
    /// By doing this everytime a character is read, changes to structure can be immediately reflected.
    let printFormatted (soFar: string) =
        let parts = parts soFar // From Terminal.fs, breaks the input into its parts
        let tokens = tokens parts // Also from Terminal.fs, groups and tags the parts by type (e.g. Code)
        writeTokens startPos tokens // Writes the types out, coloured.
    
    /// For operations that alter the current string at pos (e.g. delete) 
    /// the last line position in the total string needs to be determined.
    let lastLineStart (soFar: string) =
        let lastLineBreak = soFar.LastIndexOf("\r\n")
        if lastLineBreak = -1 then 0 else lastLineBreak + 2

    /// This recursively prompts for input from the user, producing a final string result on the reception of the Enter key.
    /// As its recursive call is always the last statement, this code is tail recursive. 
    let rec reader priorIndex (soFar: string) pos =

        // By printing out the current content of the line after every char
        // implementing backspace and delete becomes easier.
        Console.CursorVisible <- false
        Console.CursorLeft <- startPos
        Console.CursorTop <- startLine
        printFormatted soFar
        Console.CursorLeft <- startPos + pos
        Console.CursorVisible <- true
        
        // Blocks here until a key is read.
        let next = Console.ReadKey true
        
        // The users keys is evaluated as either: Enter (without Shift) meaning done, 
        // a control key like Backspace, Delete, Arrows (including history up/down using the prior commands list),
        // or, if none of the above, a character to append to the 'soFar' string.
        match next.Key with
        | ConsoleKey.Enter when next.Modifiers <> ConsoleModifiers.Shift ->
            Console.CursorVisible <- false // As reading is done, Hide the cursor.
            printfn "" // Write a final newline.
            soFar
        // Enter with shift pressed adds a new line, aligned with the prompt position.
        | ConsoleKey.Enter ->
            reader priorIndex (soFar + " \r\n ") 0
        | ConsoleKey.Backspace when Console.CursorLeft <> startPos ->
            let relPos = lastLineStart soFar + pos
            let nextSoFar = soFar.[0..relPos-2] + soFar.[relPos..]
            let nextPos = max 0 (pos - 1)
            reader priorIndex nextSoFar nextPos
        | ConsoleKey.Delete ->
            let relPos = lastLineStart soFar + pos
            let nextSoFar = if soFar = "" then soFar else soFar.[0..relPos-1] + soFar.[relPos+1..]
            reader priorIndex nextSoFar pos
        // Left and Right change the position on the current line, allowing users to insert characters.
        | ConsoleKey.LeftArrow ->
            let nextPos = max 0 (pos - 1)
            reader priorIndex soFar nextPos
        | ConsoleKey.RightArrow ->
            let nextPos = min soFar.Length (pos + 1)
            reader priorIndex soFar nextPos
        // Up and Down replace the current soFar with the relevant history item from the 'prior' list.
        | ConsoleKey.UpArrow when priorIndex < List.length prior - 1 ->
            let nextIndex = priorIndex + 1
            let nextSoFar = prior.[nextIndex]
            let nextPos = nextSoFar.Length - lastLineStart nextSoFar
            reader nextIndex nextSoFar nextPos
        | ConsoleKey.DownArrow when priorIndex > 0 ->
            let nextIndex = priorIndex - 1
            let nextSoFar = prior.[nextIndex]
            let nextPos = nextSoFar.Length - lastLineStart nextSoFar
            reader nextIndex nextSoFar nextPos
        // Like Left and Right, Home and End jumps to the start or end of the current line.
        | ConsoleKey.Home ->
            reader priorIndex soFar 0
        | ConsoleKey.End ->
            let nextPos = (soFar.Length - lastLineStart soFar)
            reader priorIndex soFar nextPos
        // Tab is complex, in that it attempts to finish a path or command given the last token in soFar.
        // Nothing is done however if the tab completion would exceed the maximum line length.
        | ConsoleKey.Tab when soFar <> "" ->
            let (newSoFar, newPos) = attemptTabCompletion soFar pos
            if pos + startPos <= Console.WindowWidth - 2 then
                reader priorIndex newSoFar newPos
            else
                reader priorIndex soFar pos
        // Finally, if none of the above and the key pressed is not a control char (e.g. Alt, Esc), it is appended.
        // Unless the line is already at max length, in which case nothing is done.
        | _ ->
            let c = next.KeyChar
            let lineStart = lastLineStart soFar
            let maxLineLength = Console.WindowWidth - (if Console.CursorTop = startLine then 3 else 2)
            if not (Char.IsControl c) && (soFar.Length - lineStart) + startPos < maxLineLength then
                let relPos = lineStart + pos
                reader priorIndex (soFar.[0..relPos-1] + string c + soFar.[relPos..]) (pos + 1)
            else
                reader priorIndex soFar pos

    reader -1 "" 0