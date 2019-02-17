/// Helpers for console interaction: setting colours, parsing input into tokens etc.
module Terminal

open System
open Model

// Creates a string of whitespace, n characters long
let whitespace n = new String(' ', n)

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
            if remainder = "" then
                match wrapped with
                | Some (c, _) -> yield (string c + soFar) // If a wrapping op was in progress, add the start token to be faithful to user input.
                | None -> if soFar <> "" then yield soFar
            else
                let c, next = remainder.[0], remainder.[1..]
                match c, wrapped with
                | '(', None when soFar = "" ->
                    let nextWrapped = Some ('(', 1)
                    yield! parts soFar nextWrapped last next
                | '(', Some ('(', n) -> // Bracket pushing.
                    let nextWrapped = Some ('(', n + 1)
                    yield! parts (soFar + "(") nextWrapped last next
                | ')', Some ('(', 1) when last <> '\\' ->
                    yield sprintf "(%s)" soFar
                    yield! parts "" None last next
                | ')', Some ('(', n) when last <> '\\' -> // Bracket popping.
                    let nextWrapped = Some ('(', n - 1)
                    yield! parts (soFar + ")") nextWrapped last next
                | '\"', None when soFar = "" -> 
                    yield! parts soFar (Some ('\"', 1)) last next // Quotes always have a 'stack' of 1, as they cant be pushed/popped like brackets.
                | '\"', Some ('\"', 1) when last <> '\\' ->
                    yield sprintf "\"%s\"" soFar
                    yield! parts "" None last next
                | ' ', None when last <> '\\' ->
                    yield soFar
                    yield! parts "" None last next
                | _ -> 
                    yield! parts (soFar + string c) wrapped c next
        ]
    let raw = parts "" None ' ' s
    if List.isEmpty raw then raw
    else
        // A final fold is used to combine whitespace blocks: e.g. "";"";"" becomes "   "
        let parsed, final = 
            (([], raw.[0]), raw.[1..]) 
            ||> List.fold (fun (results, last) next ->
                if next = "" && last = "" then 
                    results, "  "
                elif next = "" && String.IsNullOrWhiteSpace last then 
                    results, last + " "
                elif String.IsNullOrWhiteSpace last then
                    last.[..last.Length - 2]::results, next
                else 
                    last::results, next)
        List.rev (final::parsed)

/// Tokens takes a set of parts (returned by previous method - a string array) and converts it into `operational tokens`.
/// E.g. echo hello world |> (fun (s:string) -> s.ToUpper()) >> out.txt would become a [Command; Pipe; Code; Out]
let tokens partlist = 
    [
        let mutable i = 0
        let max = List.length partlist - 1
        while i <= max do
            let part = partlist.[i]
            if String.IsNullOrWhiteSpace part then 
                yield Whitespace part.Length
                i <- i + 1
            elif part = "|>" then 
                yield Pipe
                i <- i + 1
            elif part = ">>" && i < max then
                yield Out partlist.[i + 1]
                i <- i + 2
            elif part.[0] = '(' && (i = max || part.[part.Length - 1] = ')') then
                yield Code part
                i <- i + 1
            else
                let command = part
                let args = [
                    let mutable valid = true
                    i <- i + 1
                    while valid && i <= max do
                        let argOption = partlist.[i]
                        if argOption = "|>" || argOption = ">>" then
                            valid <- false
                            i <- i - 1
                        elif argOption = "" then
                            yield " "
                        else 
                            yield argOption
                        i <- i + 1
                ]
                yield Command (command, args)
    ]

/// Writes out a list of tokens to the output, coloured appropriately.
let writeTokens tokens = 
    tokens 
    |> List.iter (function 
        | Command (s, args) -> 
            colour "Yellow"
            printf "%s " s
            defaultColour ()
            args |> List.iter (printf "%s ")
        | Code s ->
            colour "Cyan"
            printf "%s " s
        | Pipe ->
            colour "Green"
            printf "|> "
        | Out s ->
            colour "Green"
            printf ">> "
            defaultColour ()
            printf "%s" s
        | Whitespace n ->
            printf "%s" (whitespace n))