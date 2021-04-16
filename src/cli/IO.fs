namespace b0wter.Fbrary

open System
open System.IO
open System.Linq
open FsToolkit.ErrorHandling
open System.Collections.Generic

module IO =
    
    let getSubDirectories (path: string) =
        if not <| Directory.Exists(path) then Error (sprintf "'%s' is not a directory." path)
        else
            Directory.GetDirectories(path) |> List.ofArray |> Ok
        
    /// Gets a list of files in the given directory and all subdirectories.
    let rec private filesFromDirectory (recursive: bool) (directory: string) : string list =
        let files = Directory.GetFiles directory |> List.ofArray
        if recursive then
            let folders = Directory.GetDirectories directory |> List.ofArray |> List.collect (filesFromDirectory recursive)
            files @ folders
        else
            files

    /// Gets a list of files in the given directory and all subdirectories.
    let listFiles (recursive: bool) (directory: string) =
        if not <| Directory.Exists(directory) then Error (sprintf "The directory '%s' does not exist." directory)
        else Ok (filesFromDirectory recursive directory)
        
    let filterMp3Files (files: string list) : string list =
        files |> List.filter (fun f ->
            let extension = Path.GetExtension(f).ToLower()
            extension.EndsWith ".mp3" || extension.EndsWith ".ogg")
        
    let removeFirstFolder (filename: string) =
        let pathRoot = Path.GetPathRoot(filename)
        if pathRoot.Length = 0 then
            let parts = filename.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
            String.Join(Path.DirectorySeparatorChar, parts.Skip(1))
        else
            filename.Substring(pathRoot.Length)
        
    let getIfMp3File (path: string) =
        if File.Exists(path) && path.EndsWith(".mp3") then Ok path
        elif File.Exists(path) then Error "The given file exists but does not have an mp3 extension."
        elif Directory.Exists(path) then Error "The given path is a directory not a file."
        else Error "The given path does not exist."
        
    let getIfDirectory (path: string) =
        if Directory.Exists(path) then Ok path
        elif File.Exists(path) then Error "The given path is a file not a directory."
        else Error "The given path does not exist."
                
    let writeTextToFile (filename: string) (text: string) : Result<unit, string> =
        try
            File.WriteAllText(filename, text) |> Ok
        with
            ex ->
                Error ex.Message
                
    let readTextFromFile (filename: string) : Result<string, string> =
        if File.Exists(filename) then
            try
                File.ReadAllText(filename) |> Ok
            with
                ex ->
                    Error ex.Message
        else
            Error (sprintf "The file '%s' does not exist." filename)

    let readLine (addNewLine: bool) (question: unit -> string) (transform: string -> Result<'a, string>) : 'a =
        if addNewLine then
            do Console.WriteLine(question())
        else
            do Console.Write(question())
            
        let rec step () =
            let input = Console.ReadLine ()
            match input |> transform with
            | Ok i -> i
            | Error e ->
                do printfn "%s Please try again." e
                step ()
        step ()

    let readYesNo (defaultCase: bool option) (question: unit -> string) : bool =
        do Console.WriteLine(question())
            
        let rec step () =
            let input = Console.ReadKey true
            match input.Key, defaultCase with
            | ConsoleKey.Y, _ -> true
            | ConsoleKey.N, _ -> false
            | ConsoleKey.Enter, Some true -> true
            | ConsoleKey.Enter, Some false -> false
            | _, _ ->
                do printfn "Invalid input. Please enter 'y' or 'n'."
                step ()
        step ()
        
    let fileExists (filename: string) : bool =
        File.Exists(filename)
        
    let fileDoesNotExist = fileExists >> not
    
    let isSamePath p1 p2 =
        Path.GetFullPath(p1) = Path.GetFullPath(p2)
        
    let move (source: string) (target: string) : Result<unit, string> =
        (*
            Rules for moving books:
            - cannot move to a non-existing folder
            - cannot move to an existing file
        *)
        let validate source target =
            if   File.Exists(target) then Error "Cannot move to an existing file."
            elif not <| Directory.Exists(target) then Error "Cannot move to a non-existing folder."
            else Ok ()
            
        try
            
            
            let mover = match Directory.Exists(source), File.Exists(source) with
                        | true, true | false, false -> failwith "Could not determine whether the path is a file or a directory."
                        | true, false -> Directory.Move
                        | false, true -> File.Move
            
            Error "not implemented"
        with
        | error -> Error error.Message
    
    let private singleSeparator = Path.PathSeparator |> string
    let private doubleSeparator = String(Path.PathSeparator, 2)
    /// Removes redundant elements from the path (e.g. `..` and `.`).
    /// Returns an absolute path.
    ///     "/a/../"             -> "/"
    ///     "/a/../.././../../." -> "/"
    ///     "/a/./b/../../c/"    -> "/c"
    [<Obsolete("This function is purely for research purposes and should not be used. Use System.IO.Path.GetFullPath instead.")>]
    let __simplifyPath (input: string) =
        let pathRoot = Path.GetPathRoot(input)
        let isAbsolutePath = not <| String.IsNullOrWhiteSpace(pathRoot)
        let input = input.Substring(pathRoot.Length)
        
        let stack = Stack<string>()
        do input.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
           |> Array.filter ((<>) ".") // "." entries can be ignored as it means "this folder"
           |> Array.rev
           |> Array.iter stack.Push
        
        let rec fold (stack: Stack<string>) (result: Stack<string>) =
            match stack.TryPop() with
            | false, _ -> result
            | true, value ->
                match value with
                | ".." ->
                    // remove the last element from the stack
                    do result.TryPop () |> ignore 
                    fold stack result
                | "." ->
                    fold stack result
                | element ->
                    do result.Push element
                    fold stack result
        
        let result = fold stack (Stack<string>())
        
        let rec joinStack accumulator (stack: Stack<string>) =
            let separator = Path.DirectorySeparatorChar |> string
            match stack.TryPop() with
            | false, _ when String.IsNullOrWhiteSpace(accumulator) -> "/"
            | false, _ -> accumulator
            | true, value ->
                let newAccumulator = $"%s{separator}%s{value}%s{accumulator}"
                joinStack newAccumulator stack
                
        let joined = (result |> joinStack String.Empty).TrimStart(Path.DirectorySeparatorChar) 
                
        if isAbsolutePath then pathRoot + joined
        else joined
        
    let simplifyPath (path: string): string =
        Path.GetFullPath(path)
        
    module File =
        let exists = File.Exists
        
    module Directory =
        let exists = Directory.Exists
        
    module Path =
        let fullPath = Path.GetFullPath
        
        let directoryName (s: string) = Path.GetDirectoryName s
        
        let combine (a, b) = Path.Combine(a, b)