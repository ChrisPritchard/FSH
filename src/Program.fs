/// The core shell loop is defined and run here.
/// It prompts the user for input, then processes the result before repeating.
/// In addition, some ancillary functions like process launching are also defined.

open System
open System.Text
open System.IO
open System.Diagnostics
open System.ComponentModel
open Model
open Builtins
open Terminal
open LineReader
open Interactive

[<EntryPoint>]
let main _ =

    cursor false
    colour "Magenta"
    printfn " -- FSH: FSharp Shell -- "
    defaultColour ()
    printf "starting FSI..." // booting FSI takes a short but noticeable amount of time
    let fsi = new Fsi ()
    printfn "done"
    printfn "For a list of commands type '?' or 'help'"

    /// Prints the prompt ('FSH' plus the working dir) and waits for then accepts input from the user.
    let prompt prior = 
        colour "Magenta"
        printf "FSH %s> " (currentDir ())
        cursor true
        defaultColour ()
        // Here is called a special function from LineReader.fs that accepts tabs and the like.
        let read = readLine prior
        cursor false
        read
   
    /// Attempts to run an executable (not a builtin like cd or dir) and to feed the result to the output.
    let launchProcess fileName args =
        let op = 
            new ProcessStartInfo(fileName, args |> String.concat " ",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false)
            |> fun i -> new Process (StartInfo = i)
                
        // all output is written into these internally mutable builders, and written out as the result of the expression
        let outBuilder = new StringBuilder ()
        let errorBuilder = new StringBuilder ()

        op.OutputDataReceived.Add(fun e -> outBuilder.AppendLine e.Data |> ignore)
        op.ErrorDataReceived.Add(fun e -> errorBuilder.AppendLine e.Data |> ignore)

        try
            op.Start () |> ignore

            op.BeginOutputReadLine ()
            op.WaitForExit ()
            op.CancelOutputRead ()

            if errorBuilder.Length <> 0 then 
                Error (errorBuilder.ToString()) 
            else 
                Ok (outBuilder.ToString())
        with
            | :? Win32Exception as ex -> // Even on linux/osx, this is the exception thrown.
                Error (sprintf "%s: %s" fileName ex.Message)
    
    /// Attempts to run either help, a builtin, or an external process based on the given command and args
    let runCommand command args =
        // Help (or ?) are special builtins, not part of the main builtin map (due to loading order).
        if command = "help" || command = "?" then
            help args
        else
            match Map.tryFind command builtinMap with
            | Some f -> 
                f args
            | None -> // If no builtin is found, try to run the users input as a execute process command.
                launchProcess command args
    
    /// Runs an interaction, which can be any valid F# code. If the interaction results in a value, it is returned ('it' in fsi).
    let runInteraction source = 
        let (result, error) = fsi.EvalInteraction source
        if error.Length > 0 then 
            Error (error |> Seq.map (fun e -> string e) |> String.concat "\r\n")
        else
            match result with
            | Choice1Of2 () -> 
                let (outResult, outError) = fsi.EvalExpression "it"
                match outError, outResult with
                | _, Choice1Of2 (Some v) -> 
                    fsi.EvalInteraction "let it = \"\"" |> ignore // clear it
                    Ok (string v.ReflectionValue)
                | _, _ -> Ok ""
            | Choice2Of2 ex -> Error ex.Message

    /// Runs an expression, which must be a function that resolves to a value. 
    /// The last result is applied into the expression as either a string or a string array
    let runExpression source (lastResult: string) = 
        let toEval = 
            if lastResult.Contains "\r\n" then
                lastResult.Split ([|"\r\n"|], StringSplitOptions.RemoveEmptyEntries)
                |> String.concat "\";\"" 
                |> fun lastResult -> sprintf "let piped = [|\"%s\"|] in (%s) piped" lastResult source
            else 
                sprintf "let piped = \"%s\" in (%s) piped" lastResult source
        let (result, error) = fsi.EvalExpression toEval
        if error.Length > 0 then 
            Error (error |> Seq.map (fun e -> string e) |> String.concat "\r\n")
        else
            match result with
            | Choice1Of2 (Some v) -> Ok (string v.ReflectionValue)
            | Choice1Of2 None -> Ok ""
            | Choice2Of2 ex -> Error ex.Message

    /// Attempts to run code as an expression. If the last result is not empty, it is set as a value that is applied to the code as a function.
    let runCode lastResult (code: string) =
        let source = 
            if code.EndsWith ')' then code.[1..code.Length-2]
            else code.[1..]

        if lastResult = "" then 
            runInteraction source
        else
            runExpression source lastResult
            

    /// The implementation of the '>> filename' token. Takes the piped in content and saves it to a file.
    let out content path = 
        try
            File.WriteAllText (path, content)
            Ok ""
        with
            | ex -> 
                Error (sprintf "Error writing to out %s: %s" path ex.Message)
    
    /// Handles running a given token, e.g. a command, pipe, code or out.
    /// Bundles the result and passes it as a Result<string, string>, which will be printed if this is the last token,
    /// Or fed to the next token in the chain if not.
    let processToken lastResult token =
        match lastResult with
        | Error _ -> lastResult
        | Ok s ->
            match token with
            | Command (name, args) ->
                let args = args @ [s]
                runCommand name args
            | Code code ->
                runCode s code
            | Pipe -> 
                lastResult
            | Out path ->
                out s path                
            | _ -> Ok ""

    /// Splits up what has been entered into a set of tokens, then runs each in turn feeding the result of the previous as the input to the next.
    /// When complete, the result is printed to the Console out in green for success or red for error (or if success with no output, then nothing).
    let processEntered (s : string) =
        if String.IsNullOrWhiteSpace s then () // nothing specified so just loop
        else 
            let parts = parts s
            let tokens = tokens parts

            let output = (Ok "", tokens) ||> List.fold processToken
            match output with 
            | Ok "" -> ()
            | Ok s -> 
                colour "Green"
                printfn "%s" s 
            | Error s -> 
                colour "Red"
                printfn "%s" s 

    /// The coreloop waits for input, runs that input, and repeats. 
    /// It also handles the special exit command, quiting the loop and thus the process.
    let rec coreLoop prior =
        let entered = prompt prior
        if entered.Trim() = "exit" then ()
        else
            processEntered entered
            coreLoop (entered::prior)

    coreLoop []

    0