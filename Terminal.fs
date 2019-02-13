/// Helpers for console interaction: setting colours, parsing input into tokens etc.
module Terminal

open System

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

/// Splits up a string into tokens, accounting for escaped spaces and quote/code wrapping,
/// e.g. '"hello" world "this is a" test\ test' would be processed as ["hello";"world";"this is a";"test test"].
let parts s =
    // The internal recursive function processes the input one char at a time via a list computation expression. 
    // This affords a good deal of control over the output, and is functional/immutable.
    let rec parts soFar wrapped last remainder = 
        [
            if remainder = "" then yield soFar
            else
                let c, next = remainder.[0], remainder.[1..]
                match c, wrapped with
                | '(', None when soFar = "" ->
                    yield! parts soFar (Some '(') last next
                | ')', Some '(' when last <> '\\' ->
                    yield sprintf "(%s)" soFar
                    yield! parts "" None last next
                | '\"', None when soFar = "" -> 
                    yield! parts soFar (Some '\"') last next
                | '\"', Some '\"' when last <> '\\' ->
                    yield sprintf "\"%s\"" soFar
                    yield! parts "" None last next
                | ' ', None when last <> '\\' ->
                    if soFar <> "" then yield soFar
                    yield! parts "" None last next
                | _ -> 
                    yield! parts (soFar + string c) wrapped c next
        ]
    parts "" None ' ' s