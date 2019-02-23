module Interactive

open Microsoft.FSharp.Compiler.Interactive.Shell
open System.IO

/// Wrapper for F# Interactive.
/// This class instantiates the FSI instance, along with hidden reader/writer streams for FSI input/output/error.
/// It provides several methods to interact with FSI or its streams in a simple fashion from outside the class.
type Fsi () =
    
    let inStream = new MemoryStream ()
    let outStream = new MemoryStream ()
    let errorStream = new MemoryStream ()

    let inReader = new StreamReader (inStream)
    let outReader = new StreamWriter (outStream)
    let errorReader = new StreamWriter (errorStream)

    let fsiInstance =
        let fsiconfig = FsiEvaluationSession.GetDefaultConfiguration()
        let args = [| "fsi.exe"; "--noninteractive" |]
        FsiEvaluationSession.Create(fsiconfig, args, inReader, outReader, errorReader);

    /// Processes an expression that must return a single value. 
    /// Can't be used for declarations unless those are used to calculate said value.
    /// However can call a declaration made by EvalInteraction.
    member __.EvalExpression code =
        let (result, error) = fsiInstance.EvalExpressionNonThrowing code
        if error.Length > 0 then 
            Error (error |> Seq.map (fun e -> string e) |> String.concat "\r\n")
        else
            match result with
            | Choice1Of2 (Some v) -> 
                match v.ReflectionValue with
                | :? string as s -> Ok s
                | :? seq<string> as sa -> Ok (String.concat "\r\n" sa)
                | o -> Ok (string o)
            | Choice1Of2 None -> Ok ""
            | Choice2Of2 ex -> Error ex.Message

    /// Processes a line as if written to the CLI of a FSI session. 
    /// On success, will attempt to return the evaluation of 'it', or an empty string if that fails
    member __.EvalInteraction code =
        let (result, error) = fsiInstance.EvalInteractionNonThrowing code
        if error.Length > 0 then 
            Error (error |> Seq.map (fun e -> string e) |> String.concat "\r\n")
        else
            match result with
            | Choice1Of2 () -> 
                match __.EvalExpression "it" with
                | Error _ -> Ok ""
                | ok -> ok
            | Choice2Of2 ex -> Error ex.Message
