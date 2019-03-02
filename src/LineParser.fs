/// Methods for parsing a line of input into its constituent tokens and parts.
/// Parts operates on a raw string, breaking it into logical chunks.
/// Tokens takes these chunks, and groups them into process-able tokens.
module LineParser

open System
open Model

/// Folds the given string list, joing ""'s into whitespace: "";"" becomes "  "
let private joinBlanks (raw: string list) =
    let parsed, final = 
        // The fold state is the set of results so far, plus the last string (the first string, initially). 
        // The fold operates over all chars but the first.
        (([], raw.[0]), raw.[1..])
        ||> List.fold (fun (results, last) next ->
            if next = "" && last = "" then 
                results, "  "
            elif next = "" && String.IsNullOrWhiteSpace last then 
                results, last + " "
            elif last = "" then
                results, next
            elif last = "\r\n" then
                last::results, next
            elif String.IsNullOrWhiteSpace last then
                last.[0..last.Length - 2]::results, next
            else 
                last::results, next)
    List.rev (final::parsed)

/// Splits up a string into tokens, accounting for escaped spaces and quote/code wrapping,
/// e.g. '"hello" world "this is a" test\ test' would be processed as ["hello";"world";"this is a";"test test"].
/// Note the use of an inner function and the 'soFarPlus' accumulator: this ensures the recursion is 'tail recursive', by making the recursive call the final call of the function.
/// Even though this is likely not necessary for command line parsing, its still a good technique to learn to avoid unexpected stack overflows.
let parts s =
    // The internal recursive function processes the input one char at a time via a list computation expression. 
    // This affords a good deal of control over the output, and is functional/immutable.
    // The parameters are: results soFar, an option type determining whether the current character is in a 'wrapped' section (e.g. '"hello"' or '(world)'),
    // what the last character was (used to check for escaping like '\"' or '\ '), whats left to process and an accumuluator function, that allows tail recursion 
    // by shifting where the soFar and recursively parsed tail/remainder is pieced together.
    let rec parts soFar wrapped last remainder acc =
        if remainder = "" then
            match wrapped with
            | Some (c, _) -> acc [(string c + soFar)] // If a wrapping op was in progress, add the start token to be faithful to user input.
            | None -> acc (if soFar <> "" then [soFar] else [])
        else
            let c, next = remainder.[0], remainder.[1..]
            match c, wrapped with
            | '(', None when soFar = "" ->
                let nextWrapped = Some ('(', 1)
                parts soFar nextWrapped last next acc
            | '(', Some ('(', n) -> // Bracket pushing.
                let nextWrapped = Some ('(', n + 1)
                parts (soFar + "(") nextWrapped last next acc
            | ')', Some ('(', 1) when last <> '\\' ->
                parts "" None last next (fun next -> acc (sprintf "(%s)" soFar::next))
            | ')', Some ('(', n) when last <> '\\' -> // Bracket popping.
                let nextWrapped = Some ('(', n - 1)
                parts (soFar + ")") nextWrapped last next acc
            | '\"', None when soFar = "" -> 
                parts soFar (Some ('\"', 1)) last next acc // Quotes always have a 'stack' of 1, as they cant be pushed/popped like brackets.
            | '\"', Some ('\"', 1) when last <> '\\' ->
                parts "" None last next (fun next -> acc (sprintf "\"%s\"" soFar::next))
            | ' ', None when last <> '\\' ->
                parts "" None last next (fun next -> acc (soFar::next))
            | _ -> 
                parts (soFar + string c) wrapped c next acc
    let raw = parts "" None ' ' s id // The blank space here, ' ', is just a dummy initial state that ensures the first char will be treated as a new token.
    if List.isEmpty raw then raw
    else joinBlanks raw // A final fold is used to combine whitespace blocks: e.g. "";"";"" becomes "   "

/// Tokens takes a set of parts (returned by previous method - a string array) and converts it into `operational tokens`.
/// E.g. echo hello world |> (fun (s:string) -> s.ToUpper()) >> out.txt would become a [Command; Pipe; Code; Out]
/// Note again the use of an inner function and the 'soFarPlus' accumulator, as above with parts
let tokens partlist =
    let rec tokens partlist acc = 
        match partlist with
        | [] -> acc []
        | head::remainder ->
            match head with 
            | "\r\n" -> 
                tokens remainder (fun next -> acc (Linebreak::next))
            | s when String.IsNullOrWhiteSpace s ->
                tokens remainder (fun next -> acc (Whitespace s.Length::next))
            | "|>" ->
                tokens remainder (fun next -> acc (Pipe::next))
            | ">>" ->
                match remainder with
                | path::_ -> acc [Out path]
                | _ -> acc [Out ""]
            | s when s.[0] = '(' && (remainder = [] || s.[s.Length - 1] = ')') ->
                tokens remainder (fun next -> acc (Code s::next))
            | command ->
                let rec findArgs list =
                    match list with
                    | [] | "|>"::_ | ">>"::_ -> []
                    | ""::remainder ->
                        " "::findArgs remainder
                    | head::remainder -> 
                        head::findArgs remainder
                let args = findArgs remainder
                tokens remainder.[args.Length..] (fun next -> acc (Command (command, args)::next))
    tokens partlist id

// Mutable version of the above. This was used first during development, but the recursive version is arguably simpler.
// The recursive version also has the advantage in that it doesn't require the incrementing of an index - something I consistently forgot
// to do whenever I modified this, and therefore caused infinite loops :D
(*
let tokens partlist = 
    [
        let mutable i = 0
        let max = List.length partlist - 1
        while i <= max do
            let part = partlist.[i]
            if part = "\r\n" then 
                yield Linebreak
                i <- i + 1
            elif String.IsNullOrWhiteSpace part then 
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
*)