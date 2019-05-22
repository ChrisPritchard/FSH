/// Contains the readLine method, and private support methods, which reimplement Console.ReadLine with support for tab completion.
/// Tab completion attempts to finish the users current token from files, directories and the builtin list.
/// As tab completion requires we intercept the readkey, we therefore need to implement
/// the rest of the console readline functionality, like arrows and backspace.
module LineReader

open System
open System.IO
open Common
open Builtins
open LineParser
open LineWriter
open Model

/// For a set of strings, will return the common start string.
let private common startIndex (candidates : string list) =
    // finds the first index that is longer than a candidate or for which two or more candidates differ
    let uncommonPoint = 
        [startIndex..Console.BufferWidth]
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
    let last = if last.StartsWith "\"" then last.[1..] else last

    let asPath = if Path.IsPathRooted last then last else Path.Combine(currentDir (), last)
    let asDirectory = Path.GetDirectoryName asPath
    let asFile = Path.GetFileName asPath

    try
        let files = Directory.GetFiles asDirectory |> Array.map Path.GetFileName |> Array.toList
        let folders = Directory.GetDirectories asDirectory |> Array.map Path.GetFileName |> Array.toList
        let allOptions = files @ folders @ List.map fst builtins

        let candidates = allOptions |> List.filter (fun n -> n.ToLower().StartsWith(asFile.ToLower()))

        if candidates.Length = 0 then 
            soFar, pos // no change
        else
            let matched = if candidates.Length = 1 then candidates.[0] else common asFile.Length candidates
            let finalDir = if asDirectory = currentDir () then "" else asDirectory
            let soFar = soFar.[0..pos-last.Length-1] + Path.Combine(finalDir, matched)
            soFar, soFar.Length
    with
        | :? IOException -> soFar, pos // Invalid path or file access, so treat as not completable. 

/// Reads a line of input from the user, enhanced for automatic tabbing and the like.
/// Prior is a list of prior input lines, used for history navigation
let readLine (prior: string list) = 
    let startPos = Console.CursorLeft
    let startLine = Console.CursorTop
    // The maximum number of lines to clear is calculated based on the prior history.
    // E.g. if there is a prior command that is four lines long, then whenever the output is printed,
    // four lines are cleared.
    let linesToClear = ""::prior |> Seq.map (fun p -> p.Split newline |> Seq.length) |> Seq.max

    /// For operations that alter the current string at pos (e.g. delete) 
    /// the last line position in the total string needs to be determined.
    let lastLineStart (soFar: string) =
        let lastLineBreak = soFar.LastIndexOf newline
        if lastLineBreak = -1 then 0 else lastLineBreak + newline.Length

    /// This recursively prompts for input from the user, producing a final string result on the reception of the Enter key.
    /// As its recursive call is always the last statement, this code is tail recursive. 
    let rec reader priorIndex (soFar: string) pos =

        // Ensure the console buffer is wide enough for our text. 
        // This change solved so, so many issues.
        let bufferLengthRequired = startPos + soFar.Length
        if bufferLengthRequired >= Console.BufferWidth then 
            Console.BufferWidth <- bufferLengthRequired + 1

        // By printing out the current content of the line after every 
        // char, implementing backspace and delete becomes easier.
        Console.CursorVisible <- false
        Console.CursorLeft <- startPos
        Console.CursorTop <- startLine
        // The printFormatted function (from the LineWriter module) converts to token types for colouring. 
        // As part of this, the last token type can be retrieved which alters how some of the keys below work (specifically tabbing, 
        // which does tab completion for commands but tab spaces for code).
        let lastTokenType = printFormatted soFar linesToClear startPos startLine
        Console.CursorLeft <- startPos + pos
        Console.CursorVisible <- true
        
        // Blocks here until a key is read.
        let next = Console.ReadKey true

        let isShiftPressed =
            [ ConsoleModifiers.Shift; ConsoleModifiers.Control; ConsoleModifiers.Alt ]
            |> List.exists (fun m -> next.Modifiers = m) 

        // The user's key is evaluated as either: Enter (without Shift/Alt/Control) meaning done, 
        // Enter with Shift/Alt/Control meaning newline
        // a control key like Backspace, Delete, Arrows (including history up/down using the prior commands list),
        // or, if none of the above, a character to append to the 'soFar' string.
        match next.Key with
        | ConsoleKey.Enter when not isShiftPressed ->
            Console.CursorVisible <- false // As reading is done, Hide the cursor.
            printfn "" // Write a final newline.
            soFar
        // Enter with shift/control/alt pressed adds a new line, aligned with the prompt position.
        | ConsoleKey.Enter ->
            reader priorIndex (soFar + newline) 0
        | ConsoleKey.Backspace when Console.CursorLeft <> startPos ->
            let relPos = lastLineStart soFar + pos
            let nextSoFar = soFar.[0..relPos-2] + soFar.[relPos..]
            let nextPos = max 0 (pos - 1)
            reader priorIndex nextSoFar nextPos
        | ConsoleKey.Delete ->
            let relPos = lastLineStart soFar + pos
            let nextSoFar = if soFar = "" || relPos = soFar.Length then soFar else soFar.[0..relPos-1] + soFar.[relPos+1..]
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
        // Tab is complex, in that if in code it adds spaces to the line, and if not in code it attempts to finish a path 
        // or command given the last token in soFar. Nothing is done if the tab completion would exceed the maximum line length.
        | ConsoleKey.Tab when soFar <> "" ->
            let newSoFar, newPos =
                match lastTokenType with
                | Some (Code code) -> 
                    if not (code.Contains newline) then soFar, pos
                    else 
                        let lineStart = lastLineStart soFar
                        let newSoFar = soFar.[..lineStart-1] + String (' ', codeSpaces) + soFar.[lineStart..]
                        newSoFar, pos + codeSpaces
                | _ ->
                    attemptTabCompletion soFar pos
            reader priorIndex soFar pos            
        // Finally, if none of the above and the key pressed is not a control char (e.g. Alt, Esc), it is appended.
        // Unless the line is already at max length, in which case nothing is done.
        | _ ->
            let c = next.KeyChar
            let lineStart = lastLineStart soFar
            if not (Char.IsControl c) then
                let relPos = lineStart + pos
                reader priorIndex (soFar.[0..relPos-1] + string c + soFar.[relPos..]) (pos + 1)
            else
                reader priorIndex soFar pos

    reader -1 "" 0