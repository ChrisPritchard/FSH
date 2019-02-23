# FSH

FSH (_F_# _Sh_ell) is a shell like CMD, Powershell or Bash that, in addition to normal shell features (folder navigation, creation, deletion, echo etc.) also supports piping and F# interactive. You can run code to declare F# functions, then use those functions as part of a piping operation with the other builtin commands. You can also just declare anonymous expressions for piping.

To demonstrate what this means, using FSH, you could type a line like:

	`echo "hello world" |> (fun s -> s.ToUpper()) >> result.txt`

Which would create a text file in the current directory called result.txt, containing the text `HELLO WORLD`

For something more advanced, you could implement a simple 'grep' command (grep is not built in to FSH by default):

	`(let grep (s:string) arr = 
		arr |> Array.filter (fun line -> line.Contains(s)))`

Note that shift+enter will go to a new line, useful for code. Tabbing (four spaces) has to be entered manually. Also, by piping arr on the second line, the line parameter does not need a type annotation.

Then use this like:

	`ls |> (grep ".dll")`

## Why?

This has been developed for the 2019 F# Applied Competition, as an educational project, over the course of about a month. As it stands, FSH is not really intended to be used in anger: while it supports a host of builtins, and is quite extensible with its FSI integration, it is missing a lot of features like, for example, numerous flags on the default builtins (ls/dir just list the local directory, but don't for example, have any flags to provide more info or multi page support etc).

If someone wanted to take this, and extend it, they are free to do so: its under MIT. But ultimately, FSH serves as an educational example of:

- Creating a shell, able to run its own commands and external executables.
- Various folder and file manipulation techniques.
- Integrating F# Interactive into a project in a useful way.

I hope it is useful to readers on the above topics :)