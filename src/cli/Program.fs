namespace b0wter.Fbrary

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
                    step (currentRow :: rowsAccumulator) (head |> (IO.reduceFilenameLength maxRowLength)) tail
                else
                    step (currentRow :: rowsAccumulator) (head + separator) tail
        
        step [] String.Empty parts
        
    let addDirectory (addConfig: Config.AddConfig) (path: string) (idGenerator: unit -> int) = 
        result {
            let! directory = IO.getIfDirectory path
            let! files = directory |> IO.listFiles
            do printfn "Found %i files" files.Length
            let mp3s = files |> IO.filterMp3Files
            do printfn "Of these %i have a mp3 extension." mp3s.Length
            let! metadata = mp3s |> List.map TagLib.readMetaData |> b0wter.FSharp.Result.all
            return! Audiobook.createFromMultiple (not <| addConfig.NonInteractive) idGenerator metadata
        }
        
    let addFile interactive (path: string) (idGenerator: unit -> int) : Result<Audiobook.Audiobook, string> =
        result {
            let! filename = IO.getIfMp3File path
            let! metadata = filename |> TagLib.readMetaData
            return Audiobook.createFromSingle interactive idGenerator metadata
        }
        
    let addPath (addConfig: Config.AddConfig) (path: string) (idGenerator: unit -> int) : Result<Audiobook.Audiobook, string> =
        if File.Exists(path) then
            addFile (not <| addConfig.NonInteractive) path idGenerator
        elif Directory.Exists(path) then
            addDirectory addConfig path idGenerator
        else
            Error "The given path does not exist."
            
    let private readLibraryOr (ifNotExisting: unit -> Library.Library) (filename: string) : Result<Library.Library, string> =
        if File.Exists(filename) then
            (IO.readTextFromFile filename) |> Result.bind Library.deserialize
        elif Directory.Exists(filename) then
            Error "The given path is a directory not a file."
        else
            ifNotExisting () |> Ok
        
    let readOrCreateLibrary = 
        readLibraryOr (fun () -> Library.empty)
        
    let readLibraryIfExisting (filename: string) : Result<Library.Library option, string> =
        if File.Exists(filename) then
            (IO.readTextFromFile filename) |> Result.bind (Library.deserialize >> Result.map Some)
        elif Directory.Exists(filename) then
            Error "The given path is a directory not a file."
        else
            Ok None

    let formattedAudiobook (format: string) (a: Audiobook.Audiobook) : string =
        a |> Formatter.CommandLine.applyAll format
        
    let idGenerator (library: Library.Library) : (unit -> int) =
        let mutable counter = if library.Audiobooks.IsEmpty then 0
                              else  library.Audiobooks |> List.map (fun a -> a.Id) |> List.max
        fun () ->
            do counter <- (counter + 1)
            counter
            
    let add (library: Library.Library) (idGenerator: (unit -> int)) (addConfig: Config.AddConfig) (config: Config.Config) : Result<int, string> =
        result {
            let! audiobook = addPath addConfig addConfig.Path idGenerator
            let! updatedLibrary = library |> Library.addBook audiobook
            do! updatedLibrary |> Library.serialize |> IO.writeTextToFile config.LibraryFile
            return 0
        }
            
    let remove (libraryFile: string) (removeConfig: Config.RemoveConfig) : Result<int, string> =
        result {
            let! library = Library.fromFile libraryFile
            let! audiobook = library |> Library.findById removeConfig.Id
            let updatedLibrary = library |> Library.removeBook audiobook
            if updatedLibrary.Audiobooks.Length = library.Audiobooks.Length then
                do printfn "No audio book has the id '%i'. The library has not been modified." removeConfig.Id
            else
                do! updatedLibrary |> Library.serialize |> IO.writeTextToFile libraryFile
            return 0
        }
        
    let list (libraryFile: string) (listConfig: Config.ListConfig) : Result<int, string> =
        let unratedPredicate = fun (a: Audiobook.Audiobook) -> a.Rating = None
        let completedPredicate = fun (a: Audiobook.Audiobook) -> a.Completed = true
        let notCompletedPredicate = fun (a: Audiobook.Audiobook) -> a.Completed = false
        let filterPredicate pattern = fun (a: Audiobook.Audiobook) -> a |> Audiobook.containsString pattern
        let idPredicate = if listConfig.Ids.IsEmpty then (fun _ -> true)
                          else fun (a: Audiobook.Audiobook) -> listConfig.Ids |> List.contains a.Id
        let predicates = match listConfig.Filter, listConfig.Unrated, listConfig.NotCompleted, listConfig.Completed with
                         | "", false, false, false -> [ fun _ -> true ]
                         | "", true, false, false -> [ unratedPredicate ]
                         | "", false, true, false -> [ notCompletedPredicate ]
                         | "", true, true, false -> [ unratedPredicate; notCompletedPredicate ]
                         | s, false, false, false -> [ filterPredicate s ]
                         | s, true, false, false -> [ filterPredicate s; unratedPredicate ]
                         | s, false, true, false -> [ filterPredicate s; notCompletedPredicate ]
                         | s, true, true, false -> [ filterPredicate s; unratedPredicate; notCompletedPredicate ]
                         | "", true, false, true -> [ unratedPredicate; completedPredicate ]
                         | "", false, true, true -> [ notCompletedPredicate; completedPredicate ]
                         | "", true, true, true -> [ unratedPredicate; notCompletedPredicate; completedPredicate ]
                         | s, false, false, true -> [ filterPredicate s; completedPredicate ]
                         | s, true, false, true -> [ filterPredicate s; unratedPredicate; completedPredicate ]
                         | s, false, true, true -> [ filterPredicate s; notCompletedPredicate; completedPredicate ]
                         | s, true, true, true -> [ filterPredicate s; unratedPredicate; notCompletedPredicate; completedPredicate ]
        let combinedPredicate a = List.forall (fun p -> a |> p) (idPredicate :: predicates)
        
        result {
            match! readLibraryIfExisting libraryFile with
            | Some l ->
                let filtered = l.Audiobooks |> List.filter combinedPredicate
                do if filtered.IsEmpty then do printfn "Found no matching audio books."
                   else do filtered |> List.iter (fun f -> printfn "%s" (f |> formattedAudiobook listConfig.Format))
                return 0
            | None ->
                return! (Error "The given library file does not exist. There is nothing to list.")
        }
        
    let update (libraryFile: string) (updateConfig: Config.UpdateConfig) : Result<int, string> =
        result {
            let! library = Library.fromFile libraryFile
            let! book = library |> Library.findById updateConfig.Id
            
            let questionFor key value = fun () -> sprintf "The current %s is '%s'. Do you want to change that? [y/N]" key value
            let readTextHint = fun () -> "Please enter the new value: "
            
            let artistQuestion = questionFor "artist" (book.Artist |?| "Empty")
            let albumQuestion = questionFor "album" (book.Album |?| "Empty")
            let albumArtistQuestion = questionFor "album artist" (book.AlbumArtist |?| "Empty")
            let titleQuestion = questionFor "title" (book.Title |?| "Empty")
            let genreQuestion = questionFor "genre" (book.Genre |?| "Empty")
            let commentQuestion = questionFor "comment" (book.Comment |?| "Empty")
            let ratingQuestion = questionFor "rating" (book.Rating |> Option.map string |?| "Empty")
            
            let updateStringProperty question current =
                if IO.readYesNo (Some false) question then Some (IO.readLine false readTextHint Ok)
                else current
                
            let updateRating current =
                let validateRatingRange i = if i > 0 && i <= 5 then Ok i else Error "The number must be greater or equal to one and less or equal to five."
                if IO.readYesNo (Some false) ratingQuestion then Some (IO.readLine false readTextHint (Utilities.Parsers.int >> Result.bind validateRatingRange))
                else current
            
            let artist = updateStringProperty artistQuestion book.Artist
            let album = updateStringProperty albumQuestion book.Album
            let albumArtist = updateStringProperty albumArtistQuestion book.AlbumArtist
            let title = updateStringProperty titleQuestion book.Title
            let genre = updateStringProperty genreQuestion book.Genre
            let comment = updateStringProperty commentQuestion book.Comment
            let rating = updateRating book.Rating
            
            let updatedBook = { book with Artist = artist; Album = album; AlbumArtist = albumArtist; Title = title; Genre = genre; Comment = comment; Rating = rating }
            let! updatedLibrary = library |> Library.updateBook updatedBook
            do! updatedLibrary |> Library.serialize |> IO.writeTextToFile libraryFile
            
            return 0           
        }
        
    let rate (libraryFile: string) (bookId: int option) : Result<int, string> =
            
        let rateSingleBook (a: Audiobook.Audiobook) : Audiobook.Audiobook =
            let input () =
                let mutable breaker = true
                let mutable rating = None
                while breaker do
                    let userInput = Console.ReadLine()
                    if userInput |> String.IsNullOrWhiteSpace then
                        do printfn "You did not enter a rating. The item will be skipped."
                        rating <- None
                        breaker <- false
                    else
                        match b0wter.FSharp.Parsers.parseInt userInput with
                        | Some int when int <= 5 && int > 0 ->
                            rating <- Some int
                            breaker <- false
                        | Some int ->
                            do printfn "The number '%i' is out of range." int
                        | None ->
                            do printfn "The input is not a valid number."
                rating
            
            let x = match (a.Artist, a.Album, a.Title) with
                    | Some artist, Some album, _ -> sprintf "'%s' (%s)" album artist
                    | Some artist, _, Some title -> sprintf "'%s' (%s)" title artist
                    | Some artist, _, _ -> sprintf "'%s' (%s)" (a.Source |> Audiobook.sourceAsString) artist
                    | _, _, _ -> sprintf "'%s'" (a.Source |> Audiobook.sourceAsString)
            let previousRatingHint = match a.Rating with
                                     | Some rating -> sprintf "(the current rating is '%i')" rating
                                     | None -> String.Empty
            do printfn "Please enter a rating from 1-5 for %s %s" x previousRatingHint
            let rating = input ()
            { a with Rating = rating }
            
        result {
            let! library = Library.fromFile libraryFile
            
            let predicate = match bookId with
                            | Some i -> fun (a: Audiobook.Audiobook) -> a.Id = i
                            | None -> fun (a: Audiobook.Audiobook) -> a.Rating = None
                            
            let books = library.Audiobooks |> Utilities.List.splitBy predicate
            
            if books.Matching.Length > 0 then
                let updatedBooks = books.Matching |> List.map rateSingleBook
                let mergedBooks = (updatedBooks @ books.NonMatching) |> List.sortBy (fun a -> a.Id)
                
                let updatedLibrary = { library with Audiobooks = mergedBooks }
                do! updatedLibrary |> Library.serialize |> IO.writeTextToFile libraryFile
            else
                let text = match bookId with
                           | Some i -> sprintf "There is no book with the id '%i'." i
                           | None -> sprintf "All books have ratings. If you want to change a rating please specify a book id."
                do printfn "%s" text                  
            return 0
        }
        
    let completedStatus libraryFile ids status = 
        result {
            let! library = Library.fromFile libraryFile
            let! books = Library.findByIds ids library
            let updatedBooks = books |> List.map (Audiobook.withCompletionStatus status)
            let! updatedLibrary = Library.updateBooks updatedBooks library
            do! updatedLibrary |> Library.serialize |> IO.writeTextToFile libraryFile
            return 0
        }
        
    let unmatched libraryFile (config: Config.UnmatchedConfig) =
        result {
            let! library = Library.fromFile libraryFile
            let allLibraryFiles = library.Audiobooks |> List.collect Audiobook.allFiles
            let! allPathFiles = config.Path |> IO.listFiles |> Result.map IO.filterMp3Files
            let missingFiles = allPathFiles |> List.except allLibraryFiles
            if missingFiles.IsEmpty then
                do printfn "All files are included in the library."
                return 0
            else
                do missingFiles |> List.iter Console.WriteLine
                return 0
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
            | Config.Remove removeConfig ->
                return! remove config.LibraryFile removeConfig
            | Config.Update updateConfig ->
                return! update config.LibraryFile updateConfig
            | Config.Rate rateConfig ->
                return! rate config.LibraryFile rateConfig.Id
            | Config.Completed completedConfig ->
                return! completedStatus config.LibraryFile completedConfig.Ids true
            | Config.NotCompleted notCompletedConfig ->
                return! completedStatus config.LibraryFile notCompletedConfig.Ids false
            | Config.Unmatched unmatchedConfig ->
                return! unmatched config.LibraryFile unmatchedConfig
            | Config.Uninitialized ->
                printfn "%s" (parser.PrintUsage())
                return 1
        }
        
        match r with
        | Ok statusCode -> statusCode
        | Error e ->
            printfn "%s" e
            1