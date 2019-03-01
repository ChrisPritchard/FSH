/// The core shell loop is defined and run here.
/// It prompts the user for input, then processes the result before repeating.
/// In addition, some ancillary functions like process launching are also defined.

open System
open System.IO
open System.Diagnostics
open System.ComponentModel
open Constants
open Model
open Builtins
open LineParser
open LineReader
open Interactive

[<EntryPoint>]
let main _ =

    Console.CursorVisible <- false
    apply Colours.title
    printfn " -- FSH: FSharp Shell -- "
    apply Colours.neutral
    printf "starting FSI..." // booting FSI takes a short but noticeable amount of time
    let fsi = new Fsi ()
    printfn "done"
    printfn "For a list of commands type '?' or 'help'"

    /// Prints the prompt ('FSH' plus the working dir) and waits for then accepts input from the user.
    let prompt prior = 
        apply Colours.prompt
        printf "%s %s> " promptName (currentDir ())
        // Here is called a special function from LineReader.fs that accepts tabs and the like.
        let read = readLine prior
        read
   
    /// Attempts to run an executable (not a builtin like cd or dir) and to feed the result to the output.
    let launchProcess fileName args writeOut writeError =
        let op = 
            new ProcessStartInfo(fileName, args |> String.concat " ",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false)
            |> fun i -> new Process (StartInfo = i)

        op.OutputDataReceived.Add(fun e -> writeOut e.Data |> ignore)
        op.ErrorDataReceived.Add(fun e -> writeError e.Data |> ignore)

        try
            op.Start () |> ignore

            op.BeginOutputReadLine ()
            op.WaitForExit ()
            op.CancelOutputRead ()
        with
            | :? Win32Exception as ex -> // Even on linux/osx, this is the exception thrown.
                writeError (sprintf "%s: %s" fileName ex.Message)
    
    /// Attempts to run either help, a builtin, or an external process based on the given command and args
    let runCommand command args writeOut writeError =
        // Help (or ?) are special builtins, not part of the main builtin map (due to loading order).
        if command = "help" || command = "?" then
            help args writeOut writeError
        else
            match Map.tryFind command builtinMap with
            | Some f -> 
                f args writeOut writeError
            | None -> // If no builtin is found, try to run the users input as a execute process command.
                launchProcess command args writeOut writeError

    /// Attempts to run code as an expression or interaction. 
    /// If the last result is not empty, it is set as a value that is applied to the code as a function.
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
                elif lastResult.Contains "\r\n" then
                    lastResult.Split ([|"\r\n"|], StringSplitOptions.RemoveEmptyEntries)
                    |> String.concat "\";\"" 
                    |> fun lastResult -> sprintf "let (piped: string[]) = [|\"%s\"|] in piped |> (%s)" lastResult source
                else 
                    sprintf "let (piped: string) = \"%s\" in piped |> (%s)" lastResult source
            // Without the type annotations above, you would need to write (fun (s:string) -> ...) rather than just (fun s -> ...)
            fsi.EvalExpression toEval writeOut writeError
            
    /// The implementation of the '>> filename' token. Takes the piped in content and saves it to a file.
    let runOut content path _ writeError = 
        try
            File.WriteAllText (path, content)
        with
            | ex -> 
                writeError (sprintf "Error writing to out %s: %s" path ex.Message)
    
    /// Handles running a given token, e.g. a command, pipe, code or out.
    /// Output is printed into string builders if intermediate tokens, or to the console out if the last.
    /// In this way, the last token can print in real time.
    let processToken isLastToken lastResult token =
        match lastResult with
        | Error _ -> lastResult
        | Ok s ->
            let output = new OutputWriter ()
            let writeOut, writeError = 
                if isLastToken then 
                    (fun (s:string) ->
                        apply Colours.goodOutput
                        Console.WriteLine s),
                    (fun (s:string) ->
                        apply Colours.errorOutput
                        Console.WriteLine s)
                else
                    output.writeOut, output.writeError

            match token with
            | Command (name, args) ->
                let args = if s <> "" then args @ [s] else args
                runCommand name args writeOut writeError
            | Code code ->
                runCode s code writeOut writeError
            | Pipe -> 
                writeOut s // last result takes the last val and sets it as the next val
            | Out path ->
                runOut s path writeOut writeError            
            | _ -> () // The Token DU also includes presentation only tokens, like linebreaks and whitespace. These are ignored.
            output.asResult ()

    /// Splits up what has been entered into a set of tokens, then runs each in turn feeding the result of the previous as the input to the next.
    /// The last token to be processed prints directly to the console out.
    let processEntered (s : string) =
        if String.IsNullOrWhiteSpace s then () // nothing specified so just loop
        else 
            let parts = parts s
            let tokens = tokens parts
            let lastToken = List.last tokens

            // The last token prints directly to the console out, and therefore the final result is ignored.
            (Ok "", tokens) 
            ||> List.fold (fun lastResult token -> 
                processToken (token = lastToken) lastResult token)
            |> ignore

    /// The coreloop waits for input, runs that input, and repeats. 
    /// It also handles the special exit command, quiting the loop and thus the process.
    let rec coreLoop prior =
        let entered = prompt prior
        if entered.Trim() = "exit" then ()
        else
            processEntered entered
            coreLoop (entered::prior)

    // Entry invocation of FSH!
    coreLoop []

    0