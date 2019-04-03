/// FSH has very little need for its own types, as its largely about translating user input into direct actions.
/// However, where custom types are needed they are defined here, along with closely related methods.
module Model

open System.Text

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
    | Out of append: bool * fileName:string
    /// Represents a chunk of whitespace, used to preserve rendering as written, mostly.
    | Whitespace of length:int
    /// Like whitespace, is used for formatting mostly
    | Linebreak 

/// OutputWriter is a log out container used to capture the output of everything except for the last token in a command chain.
/// It captures log lines using mutable internal string builders, which are then returned for the next token in the chain using the asResult member function.
type OutputWriter () =
    let out = StringBuilder ()
    let error = StringBuilder ()
    /// Non-error output content. Will be piped to the next token or console out.
    member __.writeOut s =
        out.AppendLine s |> ignore
    /// Error output content. If this is used by a token's processor, then the full token process stops and the output is written to console out.
    member __.writeError s =
        error.AppendLine s |> ignore
    /// Used for piping, produces a Result<string, string> that is evaluated to see if processing should continue, and with what.
    /// This also trims off errant new lines at the end of the content.
    member __.asResult () = 
        let out = (string out).TrimEnd ([|'\r';'\n'|])
        let error = (string error).TrimEnd ([|'\r';'\n'|])
        if error <> "" then Error error else Ok out