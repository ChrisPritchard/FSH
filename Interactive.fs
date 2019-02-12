module Interactive

open Microsoft.FSharp.Compiler.Interactive.Shell
open System.IO

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

    member __.EvalInteraction s =
        fsiInstance.EvalInteractionNonThrowing s

    member __.EvalExpression s =
        fsiInstance.EvalExpressionNonThrowing s
