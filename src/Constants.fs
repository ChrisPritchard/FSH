/// Constants like colours for the prompt, the prompt token and the like are defined here.
/// Certain utility methods like changing the font colour are also defined here, for cross-app use.
module Constants

open System

/// This is presented at the beginning of each line, e.g. FSH c:\>
let promptName = "FSH"

/// The colour scheme of the application, intended to be used with the apply method.
module Colours =
    let title = ConsoleColor.Magenta
    let prompt = ConsoleColor.Magenta
    let goodOutput = ConsoleColor.Green
    let errorOutput = ConsoleColor.Red
    let command = ConsoleColor.Yellow
    let argument = ConsoleColor.Gray
    let code = ConsoleColor.Cyan
    let pipe = ConsoleColor.Green
    let neutral = ConsoleColor.Gray

/// Sets the console foreground to the specified colour. 
/// Intended to be used with the colours module, e.g. apply Colours.prompt.
let apply colour = Console.ForegroundColor <- colour

/// When tabbing inside a code expressiom, this controls how many spaces are added.
let codeSpaces = 4