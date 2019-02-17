/// FSH has very little need for its own types, as its largely about translating user input into direct actions.
/// However, where custom types are needed they are defined here.
module Model

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