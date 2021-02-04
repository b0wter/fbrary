namespace b0wter.Audiobook

open b0wter.Audiobook.Arguments
open b0wter.Audiobook.Library

module Program =
    open Metadata
    open IO
    open Argu
    open b0wter.Audiobook.TagLib
    open System
    open System.IO
    open FsToolkit.ErrorHandling
        
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
        
    let printMetadataAsTable (data: Metadata list) =
        let timespanFormat = "h\:mm"
        let width = Console.WindowWidth
        
        let longestTag = ("Metadata".Length) :: (data
                                                 |> List.map (function Readable r -> Some (r |> Audiobook.maxPropertyLength) | Unreadable _ -> None)
                                                 |> List.choose id)
                         |> List.max
        let longestDuration = ("Duration".Length) :: (data |> List.map (durationLength timespanFormat)) |> List.max
        
        let longestFilename = ("Filename".Length) :: (data
                              |> List.map (fun d -> d |> maxFilenameLength))
                              |> List.max
                              
        let maxFilenameWidth = width - longestTag - longestDuration - 2 - 3 - 3 - 2
        let longestFilename = Math.Min(longestFilename, maxFilenameWidth)
        let separatorRow = sprintf "+%s+%s+%s+" (String('-', longestFilename + 2)) (String('-', longestTag + 2)) (String('-', longestDuration + 2))
        let trimFilename = shortenFilename maxFilenameWidth
        
        // Print header
        printfn "%s" separatorRow
        printfn "| %s | %s | %s |" ("Filename".PadRight(longestFilename)) ("Metadata".PadRight(longestTag)) ("Duration".PadRight(longestDuration))
        printfn "%s" separatorRow
        // Print rows
        data |> List.iter (printMetadata longestTag longestFilename longestDuration timespanFormat separatorRow trimFilename)
        
    let addDirectory (path: string) = 
        result {
            let! directory = getIfDirectory path
            let! files = directory |> (listFiles true)
            do printfn "Found %i files" files.Length
            let mp3s = files |> filterMp3Files
            do printfn "Of these %i have a mp3 extension." mp3s.Length
            let metadata = mp3s |> List.map readMetaData
            do metadata |> printMetadataAsTable
            let library = { Library.Library.Audiobooks = metadata |> onlyReadable; Library.Library.LastScanned = DateTime.Now }
            let serializedLibrary = library |> serialize
            do! serializedLibrary |> writeTextToFile "library.json"
        }
        
    let addFile (path: string) =
        result {
            let! filename = getIfFile path
            
        }
        
    let addFile (path: string) =
        result {
            let!
        }
        
    [<EntryPoint>]
    let main argv =
        let r = result {
            let errorHandler = ProcessExiter(colorizer = function ErrorCode.HelpText -> None | _ -> None)
            let parser = ArgumentParser.Create<Arguments.MainArgs>(errorHandler = errorHandler)
            let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)
            
            match results.GetAllResults() with
            | [ Add path ] ->
                //let! directory = path.TryGetResult(<@ AddArgs.Path @>) |> Utilities.Result.fromOption "No path parameter specified."
                return! run path
            | [ List pattern ] ->
                let pattern = match pattern with
                              | Some s when String.IsNullOrWhiteSpace(s) -> "*"
                              | Some s -> s
                              | None -> "*"
                return 0
            | _ ->
                printfn "%s" (parser.PrintUsage())
                return 1
        }
        
        match r with
        | Ok statusCode -> statusCode
        | Error e ->
            printfn "%s" e
            1