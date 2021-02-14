namespace b0wter.Fbrary

open System
open System.IO
open System.Linq
open FsToolkit.ErrorHandling

module IO =

    /// Gets a list of files in the given directory and all subdirectories.
    let rec private filesFromDirectory (directory: string) : string list =
        let files = Directory.GetFiles directory |> List.ofArray 
        let folders = Directory.GetDirectories directory |> List.ofArray |> List.collect filesFromDirectory
        files @ folders

    /// Gets a list of files in the given directory and all subdirectories.
    let listFiles (directory: string) =
        if not <| Directory.Exists(directory) then Error (sprintf "The directory '%s' does not exist." directory)
        else Ok (filesFromDirectory directory)
        
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
        
    let getIfFile (path: string) =
        if File.Exists(path) then Ok path
        elif Directory.Exists(path) then Error "The given path is a directory not a file."
        else Error "The given path does not exist."
        
    /// Shortens a filename by consecutively removing the root path.
    /// If this results in an empty string the filename without any
    /// directory information is returned. If the filename itself is too long
    /// the end is trimmed and "..." is added.
    let shortenFilename (maxLength: int) (filename: string) : string =
        if filename.Length <= maxLength then filename
        else
            let mutable f = filename
            while f.Length > maxLength do
                f <- removeFirstFolder f
            if f |> String.IsNullOrWhiteSpace then
                let filenameOnly = Path.GetFileName(filename)
                if filenameOnly.Length > maxLength then
                    filenameOnly.Substring(maxLength - 3) + "..."
                else filenameOnly
            else f
            
    let reduceFilenameLength (maxLength: int) (filename: string) =
        if filename.Length <= maxLength then filename
        else filename.Substring(0, maxLength - 3) + "..."
                
    let writeBytesToFile (filename: string) (bytes: byte []) =
        try
            File.WriteAllBytes(filename, bytes) |> Ok
        with
            ex ->
                Error ex.Message
                
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

    let readLine (question: unit -> string) (transform: string -> Result<'a, string>) : 'a =
        do Console.WriteLine(question())
        let rec step () =
            let input = Console.ReadLine ()
            match input |> transform with
            | Ok i -> i
            | Error e ->
                do printfn "%s" e
                step ()
        step ()

    let readYesNo (question: unit -> string) : bool =
        do Console.WriteLine(question())
        let rec step () =
            let input = Console.ReadKey true
            if input.Key = ConsoleKey.Y then true
            elif input.Key = ConsoleKey.N then false
            else
                do printfn "Invalid input. Please enter 'y' or 'n'."
                step ()
        step ()
        
            