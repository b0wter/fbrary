namespace b0wter.Audiobook

open System.Runtime.Serialization
open System.Runtime.Serialization.Formatters.Binary
open FsToolkit.ErrorHandling

module IO =
    open System
    open System.IO
    open System.Linq

    let rec filesFromDirectory (firstLevel: bool) (directory: string) : string list =
        let files = Directory.GetFiles directory |> List.ofArray //|> Array.map (fun d -> Path.Join(directory, d)) |> List.ofArray
        let folders = Directory.GetDirectories directory |> List.ofArray |> List.collect (filesFromDirectory false)
        files @ folders

    let listFiles (firstLevel: bool) (directory: string) =
        if not <| Directory.Exists(directory) then Error (sprintf "The directory '%s' does not exist." directory)
        else Ok (filesFromDirectory firstLevel directory)
        
    let filterMp3Files (files: string list) : string list =
        files |> List.filter (fun f -> f.EndsWith ".mp3")
        
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
        try
            File.ReadAllText(filename) |> Ok
        with
            ex ->
                Error ex.Message
