/// FSH has very little need for its own types, as its largely about translating user input into direct actions.
/// However, where custom types are needed they are defined here, along with closely related methods.
module Model

open System
open System.IO
open System.Text
open Constants

/// Token is a DU used to tag parts of a read input.
/// It is used for piping content, and cosmetically for colouring user input.
type Token = 
    /// A command is either a builtin or an external process to run.
    | Command of text:string * arguments:string list 
    /// Code is wrapped F# to be passed through the integrated Interactive process.
    | Code of string 
    /// Pipe indicates the output of what came before should be fed into what comes after.
    | Pipe 
    /// Out is a final operation to send the output of what came before to a file.
    | Out of fileName:string
    /// Represents a chunk of whitespace, used to preserve rendering as written, mostly.
    | Whitespace of length:int
    /// Like whitespace, is used for formatting mostly
    | Linebreak 

/// Output wraps two text writers: one for regular output and one for errors.
/// It is passed to token evaluators (run command, code etc) to capture their output.
/// Methods for constructing this are found below.
type Output = {
    out: TextWriter
    error: TextWriter
}

/// Creates an Output instance that outs/errors to the Console.
/// This is used for the final token in a command stream, printing the results to the user.
/// Note the use of object expressions, to override the writeline methods so that the proper colour is set.
let consoleOutput () = 
    let out, error = new StringBuilder(), new StringBuilder()
    {
        out = 
            {   
                new StringWriter(out)
                    with member __.WriteLine (s:string) = 
                                apply Colours.goodOutput
                                Console.WriteLine s
            }
        error = 
            {   
                new StringWriter(error)
                    with member __.WriteLine (s:string) = 
                                apply Colours.errorOutput
                                Console.WriteLine s
            }
    }, out, error

/// Creates an Output instance that outs/errors into string builders.
/// This is used for all tokens except the last in a command stream, so the output of each can be fed to the next.
let builderOutput () = 
    let out, error = new StringBuilder(), new StringBuilder()
    {
        out = new StringWriter(out)
        error = new StringWriter(error)
    }, out, error

/// Takes two string builders, one for regular output and one for errors (e.g. produced by builderOutput, above) and returns a Result.
/// Additionally, does some cleanup: final errant new lines are trimmed away.
let asResult out error = 
    let out = (string out).TrimEnd ([|'\r';'\n'|])
    let error = (string error).TrimEnd ([|'\r';'\n'|])
    if error <> "" then Error error else Ok out