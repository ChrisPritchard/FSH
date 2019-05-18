/// In this module is the print formatted method, which prints a line of text in its coloured, multi-line form to the output.
/// This is used by the LineReader module, re-writing the current line to the Console out every time a character is read.
module LineWriter

open System
open System.Runtime.InteropServices
open Model
open Constants
open LineParser

/// Writes out a list of tokens to the output, coloured appropriately.
/// This also ensures the print out is aligned with the prompt
let private writeTokens promptPos tokens = 
    let align () = if Console.CursorLeft < promptPos then Console.CursorLeft <- promptPos
    let printAligned (s: string) = 
        let lines = s.Split "\r\n"
        lines |> Array.iteri (fun i line -> 
            align ()
            if line <> "" then printf "%s " line
            if i <> Array.length lines - 1 then 
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
        | Out (append, fileName) ->
            apply Colours.pipe
            printAligned (if append then ">" else ">>")
            apply Colours.argument
            printAligned fileName
        | Whitespace n ->
            printAligned (String (' ', n))
        | Linebreak ->
            printfn "")

/// This ensures the output is cleared of prior characters before printing. 
/// It goes over the maximum number of lines of any prior entry, which ensures when jumping through history the output looks write.
let private clearLines linesToClear startLine startPos =
    [1..linesToClear] 
    |> Seq.iter (fun n -> 
        let clearLine = String (' ', (Console.WindowWidth - Console.CursorLeft) - 4)
        if n <> linesToClear then printfn "%s" clearLine
        else printf "%s" clearLine)
    Console.CursorTop <- startLine
    Console.CursorLeft <- startPos

/// Takes a string (single or multiline) and prints it coloured by type to the output.
/// By doing this everytime a character is read, changes to structure can be immediately reflected.
let printFormatted (soFar: string) linesToClear startPos startLine =
    let parts = parts soFar // From LineParser.fs, breaks the input into its parts
    let tokens = tokens parts // Also from LineParser.fs, groups and tags the parts by type (e.g. Code)
    
    clearLines linesToClear startLine startPos
        
    writeTokens startPos tokens // Writes the types out, coloured.
    // finally, return the last non-whitespace token (used to control the behaviour of tab).
    tokens |> List.filter (function | Whitespace _ | Linebreak -> false | _ -> true) |> List.tryLast
