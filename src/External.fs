/// Contains the two slightly complicated functions used to start external processes.
/// This module makes heavy use of Process, ProcessStartInfo and deals with std out and error streams.
module External

open System
open System.Diagnostics
open System.ComponentModel
open System.Runtime.InteropServices
open Constants

/// Attempts to run an executable (not a builtin like cd or dir), ignoring the output
/// This is used only when the process being run is the last process in the chain, and gives said process full control of the standard out
let rec launchProcessWithoutCapture fileName args =
    use op = // As Process is IDisposable, 'use' here ensures it is cleaned up.
        ProcessStartInfo(fileName, args |> String.concat " ",
            UseShellExecute = false)
        |> fun i -> new Process (StartInfo = i) // Because Process is IDisposable, we use the recommended 'new' syntax.

    Console.CursorVisible <- true // so when receiving input from the child process, it has a cursor

    try
        apply Colours.goodOutput
        op.Start () |> ignore
        op.WaitForExit ()
    with
        // Even on linux/osx, this is the exception thrown when launching failed
        | :? Win32Exception as ex ->
            // If on windows and the error the file isn't an executable, try piping through explorer.
            // This will cause explorer to query the registry for the default handler program.
            if ex.Message = notExecutableError && RuntimeInformation.IsOSPlatform OSPlatform.Windows then
                launchProcessWithoutCapture "explorer" (fileName::args)
            else
                // Will USUALLY occur when trying to run a process that doesn't exist.
                // But running something you don't have rights too will also throw this.
                apply Colours.errorOutput
                printfn "%s: %s" fileName ex.Message
    // Hide the cursor.
    Console.CursorVisible <- false

/// Attempts to run an executable (not a builtin like cd or dir) and to feed the result to the output.
/// Differs from the above in that the run processes' outputs (out and error) are 'fed' to the writeOut/writeError methods.
/// Processes that manipulate the shell (like Git Clone) won't work well with this (they'll work, but generate no output).
let rec launchProcess fileName args writeOut writeError =
    use op = // As Process is IDisposable, 'use' here ensures it is cleaned up.
        ProcessStartInfo(fileName, args |> String.concat " ",
            UseShellExecute = false,
            RedirectStandardOutput = true, // Output is redirected so it can be captured by the events below.
            RedirectStandardError = true, // Error is also redirected for capture.
            RedirectStandardInput = false) // Note we don't redirect input, so that regular console input can be sent to the process.
        |> fun i -> new Process (StartInfo = i) // Because Process is IDisposable, we use the recommended 'new' syntax.

    op.OutputDataReceived.Add(fun e -> writeOut e.Data |> ignore) // These events capture output and error, and feed them into the writeMethods.
    op.ErrorDataReceived.Add(fun e -> writeError e.Data |> ignore)
    Console.CursorVisible <- true // so when receiving input from the child process, it has a cursor

    try
        op.Start () |> ignore

        op.BeginOutputReadLine () // Necessary so that the events above will fire: the process is asynchronously listened to.
        op.WaitForExit ()
        op.CancelOutputRead ()
    with
        // Even on linux/osx, this is the exception thrown when launching failed
        | :? Win32Exception as ex ->
            // If on windows and the error the file isn't an executable, try piping through explorer.
            // This will cause explorer to query the registry for the default handler program.
            if ex.Message = notExecutableError && RuntimeInformation.IsOSPlatform OSPlatform.Windows then
                launchProcess "explorer" (fileName::args) writeOut writeError
            else
                // Will USUALLY occur when trying to run a process that doesn't exist.
                // But running something you don't have rights too will also throw this.
                writeError (sprintf "%s: %s" fileName ex.Message)
    // Hide the cursor.
    Console.CursorVisible <- false