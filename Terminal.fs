module Terminal

open System
open System.IO

let originalColour = ConsoleColor.Gray

let defaultColour () = 
    Console.ForegroundColor <- originalColour

let colour s = 
    let consoleColour = Enum.Parse (typeof<ConsoleColor>, s, true) :?> ConsoleColor
    Console.ForegroundColor <- consoleColour
    
let read () = Console.Read ()

let readLine () = Console.ReadLine ()

let cursor b = Console.CursorVisible <- b

let currentDir () = Directory.GetCurrentDirectory ()