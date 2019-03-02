/// Wrappers for F# Interactive, making it a little easier/consistent to use from the rest of FSH.
/// Contains one primary class, Fsi.
module Interactive

open Microsoft.FSharp.Compiler.Interactive.Shell
open System.IO

/// This class instantiates the FSI instance, along with hidden reader/writer streams for FSI input/output/error.
/// It provides several methods to interact with FSI or its streams in a simple fashion from outside the class.
type Fsi () =
    
    // These streams and reader/writers are needed to instantiate FSI, but are otherwise not used.
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
    member __.EvalExpression code writeOut writeError =
        let (result, error) = fsiInstance.EvalExpressionNonThrowing code
        if error.Length > 0 then 
            error |> Seq.map (fun e -> string e) |> Seq.iter writeError
        else
            match result with
            | Choice1Of2 (Some v) -> 
                match v.ReflectionValue with
                | :? string as s -> writeOut s
                | :? seq<string> as sa -> sa |> Seq.iter writeOut
                | o -> writeOut (string o)
            | Choice1Of2 None -> ()
            | Choice2Of2 ex -> writeError ex.Message

    /// Processes a line as if written to the CLI of a FSI session. 
    /// On success, will attempt to return the evaluation of 'it', or an empty string if that fails
    member __.EvalInteraction code writeOut writeError =
        let (result, error) = fsiInstance.EvalInteractionNonThrowing code
        if error.Length > 0 then 
            error |> Seq.map (fun e -> string e) |> Seq.iter writeError
        else
            match result with
            | Choice1Of2 () -> __.EvalExpression "it" writeOut (fun _ -> ()) // note the error is ignored, if say it doesnt exist.
            | Choice2Of2 ex -> writeError ex.Message
