/// The core shell loop is defined and run here.
/// It prompts the user for input, then processes the result before repeating.
/// In addition, some ancillary functions like process launching are also defined.

open System
open System.IO
open Common
open Model
open Builtins
open LineParser
open LineReader
open Interactive
open External

[<EntryPoint>]
let main _ =

    // pick up the current console colour, so we can return to it on exit
    let currentConsoleColour = Console.ForegroundColor
    // Generally, the cursor is hidden when writing text that isn't from the user. 
    // This is to prevent an ugly 'flicker'.
    Console.CursorVisible <- false  

    // Below is the opening intro and help info lines of FSH. 
    // They are invoked here so fsi can be instantiated, putting it in scope of code operations below.
   
    printc Colours.title (" -- FSH: FSharp Shell -- " + newline)
    printf "version: "; printc Colours.goodOutput ("2019.5.22.1" + newline)

    // Booting FSI takes a short but noticeable amount of time.
    printf "starting "; printc Colours.code "FSI"; printf "..."
    let fsi = Fsi ()
    printfn "done"

    printf "For a list of commands type '"
    printc Colours.command "?"
    printf "' or '"
    printc Colours.command "help"
    printfn "'"

    printf "F# code can be executed via wrapping with '"
    printc Colours.code "("
    printf "' and '"
    printc Colours.code ")"
    printfn "'"
    
    /// Attempts to run either help, a builtin, or an external process based on the given command and args
    let runCommand command args lastResult writeOut writeError =
        // Help (or ?) are special builtins, not part of the main builtin map (due to loading order).
        if command = "help" || command = "?" then
            help args writeOut writeError
        else
            match Map.tryFind command builtinMap with
            | Some f -> 
                try
                    f args writeOut writeError
                with
                | :? UnauthorizedAccessException as ex ->
                    writeError (sprintf "%s failed fully/partially with error:%s%s" command newline ex.Message)
            | None -> // If no builtin is found, try to run the users input as a execute process command.
                if lastResult then
                    launchProcessWithoutCapture command args
                else
                    launchProcess command args writeOut writeError

    /// Attempts to run code as an expression or interaction. 
    /// If the last result is not empty, it is set as a value that is applied to the code as a function parameter.
    let runCode lastResult (code: string) writeOut writeError =
        let source = 
            if code.EndsWith ')' then code.[1..code.Length-2]
            else code.[1..]

        if lastResult = "" then 
            fsi.EvalInteraction source writeOut writeError
        else
            // In the code below, the piped val is type annotated and piped into the expression
            // This reduces the need for command line code to have type annotations for string.
            let toEval = 
                if code = "(*)" then // the (*) expression in special, as it treats the piped value as code to be evaluated
                    lastResult
                elif lastResult.Contains newline then
                    lastResult.Split ([|newline|], StringSplitOptions.RemoveEmptyEntries) // Treat a multiline last result as a string array.
                    |> Array.map (fun s -> s.Replace("\"", "\\\""))
                    |> String.concat "\";\"" 
                    |> fun lastResult -> sprintf "let (piped: string[]) = [|\"%s\"|] in piped |> (%s)" lastResult source
                else // If no line breaks, the last result is piped in as a string.
                    sprintf "let (piped: string) = \"%s\" in piped |> (%s)" lastResult source
                // Without the type annotations above, you would need to write (fun (s:string) -> ...) rather than just (fun s -> ...)
            fsi.EvalExpression toEval writeOut writeError
            
    /// The implementation of the '>> filename' token. Takes the piped in content and saves it to a file.
    let runOut content append path _ writeError = 
        try
            if append then 
                File.AppendAllText (path, content)
            else
                File.WriteAllText (path, content)
        with
            | ex -> 
                writeError (sprintf "Error writing to out %s: %s" path ex.Message)
    
    /// Write methods provides the outputwriter object and write out, write error methods to be passed to a token evaluator.
    /// If this is the last token, these will print to Console out. 
    /// Otherwise the outputWriter will fill with written content, to be piped to the next token.
    let writeMethods isLastToken =
        let output = OutputWriter ()
        let writeOut, writeError =
            if isLastToken then 
                (fun (s:string) ->
                    apply Colours.goodOutput
                    printfn "%s" s),
                (fun (s:string) ->
                    apply Colours.errorOutput
                    printfn "%s" s)
            else
                output.writeOut, output.writeError
        output, writeOut, writeError

    /// Handles running a given token, e.g. a command, pipe, code or out.
    /// Output is printed into string builders if intermediate tokens, or to the console out if the last.
    /// In this way, the last token can print in real time.
    let processToken isLastToken lastResult token =
        let output, writeOut, writeError = writeMethods isLastToken
        match lastResult with
        | Error ex -> 
            if isLastToken then writeError ex // no processing occurs after the last result, so the error needs to be printed
            lastResult
        | Ok s ->
            match token with
            | Command (name, args) ->
                let args = if s <> "" then args @ [s] else args
                runCommand name args isLastToken writeOut writeError
            | Code code ->
                runCode s code writeOut writeError
            | Out (append, path) ->
                runOut s append path writeOut writeError
            | Pipe | Whitespace _ | Linebreak -> 
                // Pipe uses the writeOut function to set the next content to be the pipedin last result 
                // The Token DU also includes presentation only tokens, like linebreaks and whitespace. These do nothing and so act like pipes.
                writeOut s 
            output.asResult ()

    /// Splits up what has been entered into a set of tokens, then runs each in turn feeding the result of the previous as the input to the next.
    /// The last token to be processed prints directly to the console out.
    let executeEntered (s : string) =
        if String.IsNullOrWhiteSpace s then () // nothing specified so just loop
        else 
            let parts = parts s
            let tokens = tokens parts
            let lastToken = List.last tokens

            (Ok "", tokens) // The fold starts with an empty string as the first 'piped' value
            ||> List.fold (fun lastResult token -> 
                processToken (token = lastToken) lastResult token)
            |> ignore // The last token prints directly to the console out, and therefore the final result is ignored.

    /// The coreloop waits for input, runs that input, and repeats. 
    /// It also handles the special exit command, quiting the loop and thus the process.
    /// This function is tail call optimised, so can loop forever until 'exit' is entered.
    let rec coreLoop prior =
        apply Colours.prompt
        printf "%s %s> " promptName (currentDir ())
        // Here is called a special function from LineReader.fs that accepts tabs and the like.
        let entered = readLine prior
        if entered.Trim() = "exit" then ()
        else
            executeEntered entered
            coreLoop (entered::prior)

    // Start the core loop with no prior command history. FSH begins!
    coreLoop []

    // return to the original console colour
    Console.ForegroundColor <- currentConsoleColour
    Console.CursorVisible <- true
    0
