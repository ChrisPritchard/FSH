module Terminal.Parts

open FsUnit
open FsUnit.Xunit
open Xunit

[<Fact>]
let ``Parts parses blank as blank`` () =
    let result = parts ""
    result |> should be Empty

[<Fact>]
let ``Parts parses single as single`` () =
    let result = parts "test"
    result |> should equal ["test"]

[<Fact>]
let ``Parts parses two words`` () =
    let result = parts "test1 test2"
    result |> should equal ["test1";"test2"]

[<Fact>]
let ``Parts parses quotes properly`` () =
    let result = parts "\"test1 test2\""
    result |> should equal ["\"test1 test2\""]

[<Fact>]
let ``Parts parses brackets properly`` () =
    let result = parts "(test1 test2)"
    result |> should equal ["(test1 test2)"]

[<Fact>]
let ``Parts parses nested brackets properly`` () =
    let result = parts "(test1 (test2) test3)"
    result |> should equal ["(test1 (test2) test3)"]

[<Fact>]
let ``Parts escapes quotes`` () =
    let result = parts "\\\"test1 test2\\\""
    result |> should equal ["\\\"test1";"test2\\\""]

[<Fact>]
let ``Parts escapes spaces`` () =
    let result = parts "test1 te\ st2"
    result |> should equal ["test1";"te\ st2"]

[<Fact>]
let ``Parts treats whitespace as empty tokens`` () =
    let result = parts "test1     test2"
    result |> should equal ["test1";"   ";"test2"]