/// The core shell loop is defined and run here.
/// It prompts the user for input, then processes the result before repeating.
/// In addition, some ancillary functions like process launching are also defined.

open System.Diagnostics
open System.ComponentModel
open Terminal
open Builtins
open LineReader
open Interactive

[<EntryPoint>]
let main _ =

    cursor false
    defaultColour ()
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
                
        op.OutputDataReceived.Add(fun e -> printfn "%s" e.Data)
        op.ErrorDataReceived.Add(fun e -> printfn "%s" e.Data)

        try
            op.Start () |> ignore

            colour "Green"
            op.BeginOutputReadLine ()
            op.WaitForExit ()
            op.CancelOutputRead ()
        with
            | :? Win32Exception as ex -> // Even on linux/osx, this is the exception thrown.
                colour "Red"
                printfn "%s: %s" fileName ex.Message

    /// Tries to follow what the user is wanting to do: run a builtin, or execute a process for example.
    let processCommand (s : string) =
        if s.Length = 0 then () // no command so just loop
        else 
            let parts = parts s
            let command = parts.[0]

            // Help (or ?) are special builtins, not part of the main builtin map (due to loading order).
            if command = "help" || command = "?" then
                help parts.[1..]
            else
                match Map.tryFind command builtinMap with
                | Some f -> 
                    f parts.[1..]
                | None -> // If no builtin is found, try to run the users input as a execute process command.
                    launchProcess command parts.[1..]

    /// The coreloop waits for input, runs that input, and repeats. 
    /// It also handles the special exit command, quiting the loop and thus the process.
    let rec coreLoop prior =
        let entered = prompt prior
        if entered.Trim() = "exit" then ()
        else
            processCommand entered
            coreLoop (entered::prior)
    
    //let fsi = new Fsi ()
    //let result = fsi.EvalExpression("10")
    //let result2 = fsi.EvalExpression("let x () = 12 in x ()")
    //let result3 = fsi.EvalInteraction("50")
    ////let result4 = fsi.EvalInteraction("printfn \"%i\" it")
    //let result5 = fsi.EvalExpression("it")

    //let result6 = fsi.EvalExpression """
    //    let test x = x * x
    //    test 23
    //"""

    //let result7 = fsi.EvalExpression "test 46"

    coreLoop []

    0
