/// Helpers for console interaction: setting colours, parsing input into tokens etc.
module Terminal

open System
open Builtins
open System.IO

/// The starting console colour, before it is overriden by prompts, outputs and help for example.
let originalColour = ConsoleColor.Gray

/// Resets the interface to use the default font colour.
let defaultColour () = 
    Console.ForegroundColor <- originalColour

/// Sets the console foreground colour (font colour) to the colour specified by the given string,
/// e.g. colour "Red" will set the foreground to ConsoleColor.Red.
/// String parsing is only used because its more concise than using the built in enum accessor.
let colour s = 
    let consoleColour = Enum.Parse (typeof<ConsoleColor>, s, true) :?> ConsoleColor
    Console.ForegroundColor <- consoleColour

/// Controls cursor visibility. 
/// The cursor should only be visible when accepting input from the user, and not when drawing the prompt, for example.
let cursor b = Console.CursorVisible <- b

/// Splits up a string into tokens, accounting for escaped spaces and quote wrapping,
/// e.g. '"hello" world "this is a" test\ test' would be processed as ["hello";"world";"this is a";"test test"].
let parts s =
    // The internal recursive function processes the input one char at a time via a list computation expression. 
    // This affords a good deal of control over the output, and is functional/immutable.
    // A slightly simpler way of doing this would be to use a loop with mutables; a commented out version of such an approach is below.
    let rec parts soFar quoted last remainder = 
        [
            if remainder = "" then yield soFar
            else
                let c, next = remainder.[0], remainder.[1..]
                match c with
                | '\"' when soFar = "" -> 
                    yield! parts soFar true last next
                | '\"' when last <> '\\' && quoted ->
                    yield soFar
                    yield! parts "" false last next
                | ' ' when last <> '\\' && not quoted ->
                    if soFar <> "" then yield soFar
                    yield! parts "" quoted last next
                | _ -> 
                    yield! parts (soFar + string c) quoted c next
        ]
    parts "" false ' ' s

// Mutable version of parts above, done with a loop instead of recursion.
(*
let parts s = 
    [
        let mutable last = ' '
        let mutable quoted = false
        let mutable soFar = ""

        for c in s do
            match c with
            | '\"' when soFar = "" -> quoted <- true
            | '\"' when last <> '\\' && quoted ->
                yield soFar
                quoted <- false
                soFar <- ""
            | ' ' when last <> '\\' && not quoted ->
                if soFar <> "" then yield soFar
                soFar <- ""
            | _ -> 
                soFar <- soFar + string c
                last <- c
            
        if soFar <> "" then yield soFar
    ]*)