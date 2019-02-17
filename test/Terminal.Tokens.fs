module Terminal.Tokens

open FsUnit
open FsUnit.Xunit
open Xunit
open Model

[<Fact>]
let ``Tokens parses blank as blank`` () =
    let result = tokens []
    result |> should be Empty

[<Fact>]
let ``Tokens parses no arg command`` () =
    let result = tokens ["test"]
    result |> should equal [Command ("test", [])]

[<Fact>]
let ``Tokens parses a command with args`` () =
    let result = tokens ["test";"a1";"a2"]
    result |> should equal [Command ("test", ["a1";"a2"])]
    
[<Fact>]
let ``Tokens parses code into a pipe into code`` () =
    let result = tokens ["(10)";"|>";"(printfn \"%i\")"]
    result |> should equal [Code "(10)";Pipe;Code "(printfn \"%i\")"]