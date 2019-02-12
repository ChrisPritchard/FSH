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

    /// Processes a line as if written to the CLI of a FSI session. 
    /// On success returns Unit, so can't be used for output short of reading the stream.
    member __.EvalInteraction s =
        fsiInstance.EvalInteractionNonThrowing s

    /// Processes an expression that must return a single value. 
    /// Can't be used for declarations unless those are used to calculate said value.
    /// However can call a declaration made by EvalInteraction.
    member __.EvalExpression s =
        fsiInstance.EvalExpressionNonThrowing s
