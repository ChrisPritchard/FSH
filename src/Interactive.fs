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

    /// This function takes a FsiValue (the result of a expression evaluation) and parses it for printing
    /// If a string this is simple, if a sequence of strings then each is written out individually,
    /// otherwise it just writes out a straight string conversion
    let printFsiValue writeOut (v: FsiValue) =
        match v.ReflectionValue with
        | :? string as s -> writeOut s
        | :? seq<string> as sa -> sa |> Seq.iter writeOut
        | o -> writeOut (string o)

    /// For interactions, which don't result in an Fsi value but which might set the 'it' Fsi value,
    /// this function attempts to print 'it'. If it succeeds in doing so, 'it' is then blanked so that,
    /// future interactions that don't set 'it', don't mistakingly print an earlier set 'it'. it it it it it.
    let evaluateIt writeOut =
        let (result, error) = fsiInstance.EvalExpressionNonThrowing "it"
        if error.Length = 0 then
            match result with 
            | Choice1Of2 (Some v) ->
                if v.ReflectionValue = box "" then ()
                else printFsiValue writeOut v
                fsiInstance.EvalInteractionNonThrowing "let it = \"\"" |> ignore
            | _ -> ()

    /// Processes an expression that must return a single value. 
    /// Can't be used for declarations unless those are used to calculate said value.
    /// However can call a declaration made by EvalInteraction.
    member __.EvalExpression code writeOut writeError =
        let (result, error) = fsiInstance.EvalExpressionNonThrowing code
        if error.Length > 0 then 
            error |> Seq.map (fun e -> string e) |> Seq.iter writeError
        else
            match result with
            | Choice1Of2 (Some v) -> printFsiValue writeOut v
            | Choice1Of2 None -> ()
            | Choice2Of2 ex -> writeError ex.Message

    /// Processes a line as if written to the CLI of a FSI session. 
    /// On success, will attempt to return the evaluation of 'it'.
    member __.EvalInteraction code writeOut writeError =
        let (result, error) = fsiInstance.EvalInteractionNonThrowing code
        if error.Length > 0 then 
            error |> Seq.map (fun e -> string e) |> Seq.iter writeError
        else
            match result with
            | Choice1Of2 () -> evaluateIt writeOut
            | Choice2Of2 ex -> writeError ex.Message
