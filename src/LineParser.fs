/// Methods for parsing a line of input into its constituent tokens and parts
module LineParser

open System
open Model

// Creates a string of whitespace, n characters long
let whitespace n = new String(' ', n)

/// Folds the given string list, joing ""'s into whitespace: "";"" becomes "  "
let private joinBlanks (raw: string list) =
    let parsed, final = 
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
let parts s =
    // The internal recursive function processes the input one char at a time via a list computation expression. 
    // This affords a good deal of control over the output, and is functional/immutable.
    let rec parts soFar wrapped last remainder =
        if remainder = "" then
            match wrapped with
            | Some (c, _) -> [(string c + soFar)] // If a wrapping op was in progress, add the start token to be faithful to user input.
            | None -> if soFar <> "" then [soFar] else []
        else
            let c, next = remainder.[0], remainder.[1..]
            match c, wrapped with
            | '(', None when soFar = "" ->
                let nextWrapped = Some ('(', 1)
                parts soFar nextWrapped last next
            | '(', Some ('(', n) -> // Bracket pushing.
                let nextWrapped = Some ('(', n + 1)
                parts (soFar + "(") nextWrapped last next
            | ')', Some ('(', 1) when last <> '\\' ->
                sprintf "(%s)" soFar::parts "" None last next
            | ')', Some ('(', n) when last <> '\\' -> // Bracket popping.
                let nextWrapped = Some ('(', n - 1)
                parts (soFar + ")") nextWrapped last next
            | '\"', None when soFar = "" -> 
                parts soFar (Some ('\"', 1)) last next // Quotes always have a 'stack' of 1, as they cant be pushed/popped like brackets.
            | '\"', Some ('\"', 1) when last <> '\\' ->
                sprintf "\"%s\"" soFar::parts "" None last next
            | ' ', None when last <> '\\' ->
                soFar::parts "" None last next
            | _ -> 
                parts (soFar + string c) wrapped c next
    let raw = parts "" None ' ' s
    if List.isEmpty raw then raw
    else joinBlanks raw // A final fold is used to combine whitespace blocks: e.g. "";"";"" becomes "   "

/// Tokens takes a set of parts (returned by previous method - a string array) and converts it into `operational tokens`.
/// E.g. echo hello world |> (fun (s:string) -> s.ToUpper()) >> out.txt would become a [Command; Pipe; Code; Out]
let rec tokens partlist = 
    match partlist with
    | [] -> []
    | head::remainder ->
        match head with 
        | "\r\n" -> 
            Linebreak::tokens remainder
        | s when String.IsNullOrWhiteSpace s ->
            Whitespace s.Length::tokens remainder
        | "|>" ->
            Pipe::tokens remainder
        | ">>" ->
            match remainder with
            | path::_ -> [Out path]
            | _ -> [Out ""]
        | s when s.[0] = '(' && (remainder = [] || s.[s.Length - 1] = ')') ->
            Code s::tokens remainder
        | command ->
            let rec findArgs list =
                match list with
                | [] | "|>"::_ | ">>"::_ -> []
                | ""::remainder ->
                    " "::findArgs remainder
                | head::remainder -> 
                    head::findArgs remainder
            let args = findArgs remainder
            Command (command, args)::tokens remainder.[args.Length..]

// Mutable version of the above. This was used first, during development, but the recursive version is arguably simpler.
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