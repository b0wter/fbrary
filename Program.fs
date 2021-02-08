namespace b0wter.AudiobookLibrary

open IO
open Argu
open System
open System.IO
open FsToolkit.ErrorHandling
open b0wter.FSharp.Operators
 
module Program =
        
    let getDirectoryFromArgs (argv: string []) =
        if argv.Length = 1 then Ok argv.[0]
        else Error "This program takes exactly one argument, the directory to recursively scan for mp3 files."
        
    let filenameInRows (maxRowLength: int) (filename: string) : string list =
        let pathRoot = Path.GetPathRoot filename
        let filename = filename.Substring(pathRoot.Length)
        let parts =
            if String.IsNullOrWhiteSpace pathRoot then
                filename.Split(Path.DirectorySeparatorChar) |> List.ofArray
            else
                pathRoot :: (filename.Split(Path.DirectorySeparatorChar) |> List.ofArray)
        let separator = Path.DirectorySeparatorChar.ToString()
        let separatorLength = separator.Length
        
        let rec step (rowsAccumulator: string list) (currentRow: string) (remainingParts: string list) : string list =
            match remainingParts with
            | [] -> (currentRow :: rowsAccumulator) |> List.rev |> List.filter (not << String.IsNullOrWhiteSpace)
            | head :: tail ->
                if currentRow.Length + head.Length + separatorLength <= maxRowLength then
                    step rowsAccumulator (currentRow + head + separator) tail
                elif head.Length > maxRowLength then
                    step (currentRow :: rowsAccumulator) (head |> (reduceFilenameLength maxRowLength)) tail
                else
                    step (currentRow :: rowsAccumulator) (head + separator) tail
        
        step [] String.Empty parts
        
    let addDirectory (config: Config.Config) (path: string) (idGenerator: unit -> int) = 
        result {
            let! directory = getIfDirectory path
            let! files = directory |> listFiles
            do printfn "Found %i files" files.Length
            let mp3s = files |> filterMp3Files
            do printfn "Of these %i have a mp3 extension." mp3s.Length
            let! metadata = mp3s |> List.map TagLib.readMetaData |> b0wter.FSharp.Result.all
            return! Audiobook.createFromMultiple config idGenerator metadata
        }
        
    let addFile (config: Config.Config) (path: string) (idGenerator: unit -> int) : Result<Audiobook.Audiobook, string> =
        result {
            let! filename = getIfMp3File path
            let! metadata = filename |> TagLib.readMetaData
            return Audiobook.createFromSingle config idGenerator metadata
        }
        
    let addPath (config: Config.Config) (path: string) (idGenerator: unit -> int) : Result<Audiobook.Audiobook, string> =
        if File.Exists(path) then
            addFile config path idGenerator
        elif Directory.Exists(path) then
            addDirectory config path idGenerator
        else
            Error "The given path does not exist."
            
    let private readLibraryOr (ifNotExisting: unit -> Library.Library) (filename: string) : Result<Library.Library, string> =
        if File.Exists(filename) then
            (readTextFromFile filename) |> Result.bind Library.deserialize
        elif Directory.Exists(filename) then
            Error "The given path is a directory not a file."
        else
            ifNotExisting () |> Ok
        
    let readOrCreateLibrary = 
        readLibraryOr (fun () -> Library.empty)
        
    let readLibraryIfExisting (filename: string) : Result<Library.Library option, string> =
        if File.Exists(filename) then
            (readTextFromFile filename) |> Result.bind (Library.deserialize >> Result.map Some)
        elif Directory.Exists(filename) then
            Error "The given path is a directory not a file."
        else
            Ok None
            
    let formattedAudiobook (format: string) (a: Audiobook.Audiobook) : string =
        format
            .Replace(Arguments.artistFormatString, a.Artist |?| "<unknown artist>")
            .Replace(Arguments.albumFormatString, a.Album |?| "<unknown album>")
            .Replace(Arguments.titleFormatString, a.Title |?| "<unknown title>")
            .Replace(Arguments.durationFormatString, a.Duration.ToString("h\:mm"))
            .Replace(Arguments.idFormatString, a.Id.ToString())
        
    let idGenerator (library: Library.Library) : (unit -> int) =
        let mutable counter = library.Audiobooks |> List.map (fun a -> a.Id) |> List.max
        fun () ->
            do counter <- (counter + 1)
            counter
            
    let add (library: Library.Library) (idGenerator: (unit -> int)) (addConfig: Config.AddConfig) (config: Config.Config) : Result<int, string> =
        result {
            let! audiobook = addPath config addConfig.Path idGenerator
            let! updatedLibrary = library |> Library.addBook audiobook
            do! updatedLibrary |> Library.serialize |> writeTextToFile config.LibraryFile
            return 0
        }
        
    let list (libraryFile: string) (listConfig: Config.ListConfig) : Result<int, string> =
        result {
            match! readLibraryIfExisting libraryFile with
            | Some l ->
                let filtered = if listConfig.Filter |> String.IsNullOrWhiteSpace then l.Audiobooks
                               else l.Audiobooks |> List.filter (Audiobook.containsString listConfig.Filter)
                do if filtered.IsEmpty then do printfn "No audiobook contained '%s' in its metadata." listConfig.Filter
                   else do filtered |> List.iter (fun f -> printfn "%s" (f |> formattedAudiobook listConfig.Format))
                return 0
            | None ->
                return! (Error "The given library file does not exist. There is nothing to list.")
        }
    
    [<EntryPoint>]
    let main argv =
        let r = result {
            let errorHandler = ProcessExiter(colorizer = function ErrorCode.HelpText -> None | _ -> None)
            let parser = ArgumentParser.Create<Arguments.MainArgs>(errorHandler = errorHandler)
            let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)
            let config = results.GetAllResults() |> List.fold Config.applyMainArg Config.empty
            
            match config.Command with
            | Config.Add addConfig ->
                let! library = readOrCreateLibrary config.LibraryFile
                let idGenerator = idGenerator library
                return! add library idGenerator addConfig config
            | Config.List listConfig ->
                return! list config.LibraryFile listConfig
            | Config.Rescan rescanConfig ->
                failwith "not implemented"
                return 1
            | Config.Update updateConfig ->
                failwith "not implemented"
                return 1
            | Config.Uninitialized ->
                printfn "%s" (parser.PrintUsage())
                return 1
        }
        
        match r with
        | Ok statusCode -> statusCode
        | Error e ->
            printfn "%s" e
            1