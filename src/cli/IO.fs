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
        let simplified = Path.GetFullPath path
        if simplified = (Path.DirectorySeparatorChar |> string) || simplified = Path.GetPathRoot(path) then simplified
        else simplified.TrimEnd(Path.DirectorySeparatorChar)
        
    /// Finds the largest common path across multiple paths.
    let findLargestCommonPath (paths: string list) : string =
        let directories = paths |> List.map Path.GetDirectoryName
        let shortestDirectory = directories |> List.minBy (fun d -> d.Length)
        
        let rec step (current: string) =
            if directories |> List.forall (fun d -> d.StartsWith(current)) then
                current
            else
                step (Directory.GetParent(current).FullName)
        
        step shortestDirectory
        
    module Path =
        let fullPath = Path.GetFullPath
        
        let directoryName (s: string) = Path.GetDirectoryName s
        
        let combine (a, b) = Path.Combine(a, b)
        
        let parent (path: string) : string option =
            try
                if File.Exists(path) then
                    Path.GetDirectoryName(path) |> Some
                else
                    Directory.GetParent(path).FullName |> Some
            with
            | _ -> None
        
    module File =
        let exists = File.Exists
        
        let move source target =
            try
                File.Move(source, target)
                Ok ()
            with
            | ex -> Error ex.Message
            
        /// Returns the extension of the given filename (including ".").
        /// Returns `None` if the file does not exist or does not have an extension.
        /// Returns `Some extension` otherwise.
        let extension (filename: string) =
            match Path.GetExtension(filename) with
            | null -> None
            | s when String.IsNullOrWhiteSpace(s) -> None
            | s -> Some s
            
        let hasExtension filename =
            match filename |> extension with
            | Some _ -> true
            | None -> false
            
        let fileName (s: string) = Path.GetFileName s
        
    module Directory =
        let exists = Directory.Exists
        
        let create path =
            try
                do Directory.CreateDirectory(path) |> ignore
                Ok ()
            with
            | ex -> Error ex.Message
            
        let move source target =
            try
                Directory.Move(source, target)   
                Ok ()
            with
            | ex -> Error ex.Message
            
        let moveFilesInFolder source target =
            // If the target is not a file it either:
            //  - does not exist
            //  - is a directory
            if not <| (target |> File.exists) then
                try
                    let files = Directory.GetFiles(source) |> List.ofArray
                    let fileOperation = fun files -> files |> List.traverseResultM (fun file -> File.move file target) |> Result.map (fun _ -> ())
                    let directories = Directory.GetDirectories(source) |> List.ofArray
                    let directoryOperation = fun directories -> directories |> List.traverseResultM (fun folder -> move folder target) |> Result.map (fun _ -> ())
                    
                    // Try to create the directory because it won't fail if it already exists.
                    do Directory.CreateDirectory target |> ignore
                    
                    match files |> fileOperation, directories |> directoryOperation with
                    | Ok _, Ok _ -> Ok ()
                    | Ok _, Error e -> Error (sprintf "The move operation succeeded only partially. The files were moved but the folders were not, because %s" e)
                    | Error e, Ok _ -> Error (sprintf "The move operation succeeded only partially. The files were moved but the folders were not, because %s." e)
                    | Error e1, Error e2 -> Error (sprintf "The move operation failed becaues '%s' and '%s'." e1 e2)
                with
                | ex -> Error ex.Message
            else
                Error "The target for moving multiple files needs to be a folder."
                
        let directoryName (s: string) = Path.GetDirectoryName(s)