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
        
    let addDirectory (path: string) = 
        result {
            let! directory = getIfDirectory path
            let! files = directory |> listFiles
            do printfn "Found %i files" files.Length
            let mp3s = files |> filterMp3Files
            do printfn "Of these %i have a mp3 extension." mp3s.Length
            let! metadata = mp3s |> List.map TagLib.readMetaData |> b0wter.FSharp.Result.all
            return! Audiobook.createFromMultiple metadata
        }
        
    let addFile (path: string) : Result<Audiobook.Audiobook, string> =
        result {
            let! filename = getIfMp3File path
            let! metadata = filename |> TagLib.readMetaData
            return Audiobook.createFromSingle metadata
        }
        
    let addPath (path: string) : Result<Audiobook.Audiobook, string> =
        if File.Exists(path) then
            addFile path
        elif Directory.Exists(path) then
            addDirectory path
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
            .Replace(Arguments.artistFormatString, a.Artists |?| "<unknown artist>")
            .Replace(Arguments.albumFormatString, a.Album |?| "<unknown album>")
            .Replace(Arguments.titleFormatString, a.Title |?| "<unknown title>")
            .Replace(Arguments.durationFormatString, a.Duration.ToString("h\:mm"))
        
    [<EntryPoint>]
    let main argv =
        let r = result {
            let errorHandler = ProcessExiter(colorizer = function ErrorCode.HelpText -> None | _ -> None)
            let parser = ArgumentParser.Create<Arguments.MainArgs>(errorHandler = errorHandler)
            let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)
            let format = results.TryGetResult(<@ Arguments.Format @>) |> Option.flatten |?| Arguments.defaultFormatString
            
            match results.GetAllResults() with
            | [ Arguments.Library libraryFile; Arguments.Add path ] ->
                let! library = readOrCreateLibrary libraryFile
                let! audiobook = addPath path
                let updatedLibrary = library |> Library.addBook audiobook
                do! updatedLibrary |> Library.serialize |> writeTextToFile libraryFile
                return 0
            | [ Arguments.Add _ ] ->
                do printfn "The library parameter is missing."
                printfn "%s" (parser.PrintUsage())
                return 1
            | [ Arguments.Library libraryFile; Arguments.List pattern ]
            | [ Arguments.Library libraryFile; Arguments.Format _; Arguments.List pattern ]
            | [ Arguments.Format _; Arguments.Library libraryFile; Arguments.List pattern ] ->
                match! readLibraryIfExisting libraryFile with
                | Some l ->               
                    let pattern = match pattern with
                                  | Some "*" -> ""
                                  | Some s -> s
                                  | None -> ""
                    let filtered = l.Audiobooks |> List.filter (Audiobook.containsString pattern)
                    do if filtered.IsEmpty then do printfn "No audiobook contained '%s' in its metadata." pattern
                       else do filtered |> List.iter (fun f -> printfn "%s" (f |> formattedAudiobook format))
                    return 0
                | None ->
                    return! (Error "The given library file does not exist. There is nothing to list.")
            | [ Arguments.List _ ] ->
                do printfn "The library parameter is missing."
                printfn "%s" (parser.PrintUsage())
                return 1
            | _ ->
                printfn "%s" (parser.PrintUsage())
                return 1
        }
        
        match r with
        | Ok statusCode -> statusCode
        | Error e ->
            printfn "%s" e
            1