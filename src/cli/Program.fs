namespace b0wter.Fbrary

open Argu
open System
open System.IO
open FsToolkit.ErrorHandling
open b0wter.FSharp.Operators
open b0wter.Fbrary
open b0wter.Fbrary.Arguments
open b0wter.Fbrary.Audiobook
open b0wter.Fbrary.Utilities
 
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
        let getRegular path =
            IO.getIfDirectory path |> Result.bind (IO.listFiles true) |> Result.map List.singleton
            
        let getFilesAsBooks path =
            IO.getIfDirectory path |> Result.bind (IO.listFiles false) |> Result.map(List.map List.singleton)
            
        let getSubFoldersAsBooks path =
            result {
                let! directory = IO.getIfDirectory path
                let! subdirectories = directory |> IO.getSubDirectories
                return! subdirectories |> List.map (IO.listFiles true) |> List.sequenceResultM
            }
            
        let getFilesAsBookAndSubFoldersAsBooks path =
            result {
                let! subfolderBooks = getSubFoldersAsBooks path
                let! filesBooks = getFilesAsBooks path
                return subfolderBooks @ filesBooks
            }
            
        let createBook (files: string list) =
            result {
                let! metadata = files |> List.map TagLib.readMetaData |> List.sequenceResultM
                match metadata with
                | [ single ] ->
                    return Audiobook.createFromSingle (not <| addConfig.NonInteractive) idGenerator single
                | many ->
                    return! many |> Audiobook.createFromMultiple (not <| addConfig.NonInteractive) idGenerator 
            }
        
        result {
            let! files = match addConfig.FilesAsBooks, addConfig.SubDirectoriesAsBooks with
                         | false, false -> getRegular path
                         | true, false -> getFilesAsBooks path
                         | false, true -> getSubFoldersAsBooks path
                         | true, true -> getFilesAsBookAndSubFoldersAsBooks path
            let files = files |> List.map (IO.filterMp3Files) |> List.skipEmpty
            return! files |> List.map createBook |> List.sequenceResultM
        }
        
    let addFile interactive (path: string) (idGenerator: unit -> int) : Result<Audiobook.Audiobook, string> =
        result {
            let! filename = IO.getIfMp3File path
            let! metadata = filename |> TagLib.readMetaData
            return Audiobook.createFromSingle interactive idGenerator metadata
        }
        
    let addPath (addConfig: Config.AddConfig) (path: string) (idGenerator: unit -> int) : Result<Audiobook.Audiobook list, string> =
        if File.Exists(path) then
            if addConfig.SubDirectoriesAsBooks then printfn "The option to add subdirectories as books is ignored if the given path points to a file."
            elif addConfig.FilesAsBooks then printfn "The option to add files as independent books is ignored if the given path points to a file."
            else ()
            addFile (not <| addConfig.NonInteractive) path idGenerator |> Result.map List.singleton
        elif Directory.Exists(path) then
            addDirectory addConfig path idGenerator
        else
            Error <| sprintf "The given path '%s' does not exist." path
            
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

    (*
    let formattedAudiobook (maxColumnWidth: int) (format: string option) (table: string option) (htmlTemplate: string option) (books: Audiobook.Audiobook list) : string list =
        match format, table with
        | Some _, Some t ->
            do printfn "You have set a format and specified the table option. The format specifier is ignored."
            books |> Formatter.Table.apply maxColumnWidth t
        | Some f, None ->
            books |> List.map (Formatter.CommandLine.applyAll f)
        | None, Some t ->
            books |> Formatter.Table.apply maxColumnWidth t
        | None, None ->
            books |> List.map (Formatter.CommandLine.applyAll Formatter.CommandLine.defaultFormatString)
    *)
    
    let formattedAudiobook (formatter: FormatterArgs option) (books: Audiobook.Audiobook list) : string list =
        match formatter with
        | Some (FormatterArgs.CliFormatter f) ->
            books |> List.map (Formatter.CommandLine.applyAll f)
            
    let idGenerator (library: Library.Library) : (unit -> int) =
        let mutable counter = if library.Audiobooks.IsEmpty then 0
                              else  library.Audiobooks |> List.map (fun a -> a.Id) |> List.max
        fun () ->
            do counter <- (counter + 1)
            counter
            
    let add (library: Library.Library) (idGenerator: (unit -> int)) (addConfig: Config.AddConfig) (config: Config.Config) : Result<int, string> =
        result {
            let! audiobooks = addPath addConfig addConfig.Path idGenerator
            
            let folder = fun (lib: Result<Library.Library, string>) (book: Audiobook.Audiobook) ->
                lib |> Result.bind (Library.addBook book)
            let! updatedLibrary = audiobooks |> List.fold folder (Ok library)
            
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
        let completedPredicate = fun (a: Audiobook.Audiobook) -> a.State = Audiobook.State.Completed
        let notCompletedPredicate = fun (a: Audiobook.Audiobook) -> a.State = Audiobook.State.NotCompleted
        let filterPredicate (pattern: string) = fun (a: Audiobook.Audiobook) -> a |> Audiobook.containsString (pattern.ToLower())
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
                   else do
                       filtered |> formattedAudiobook listConfig.Formatter |> List.iter (printfn "%s")
                       //filtered |> formattedAudiobook listConfig.MaxTableColumnWidth listConfig.Format listConfig.Table |> List.iter (printfn "%s")
                return 0
            | None ->
                return! (Error "The given library file does not exist. There is nothing to list.")
        }
        
    let updateInteractively (library: Library.Library) (id: int) : Result<Library.Library, string> =
        result {
            do printfn "Updating library entry #%i" id
            let! book = library |> Library.findById id
            
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
                if IO.readYesNo (Some false) ratingQuestion then Some (IO.readLine false readTextHint (Parsers.int >> Result.bind (Rating.create Ok Error)))
                else current
            
            let artist = updateStringProperty artistQuestion book.Artist
            let album = updateStringProperty albumQuestion book.Album
            let albumArtist = updateStringProperty albumArtistQuestion book.AlbumArtist
            let title = updateStringProperty titleQuestion book.Title
            let genre = updateStringProperty genreQuestion book.Genre
            let comment = updateStringProperty commentQuestion book.Comment
            let rating = updateRating book.Rating
            
            let updatedBook = { book with Artist = artist; Album = album; AlbumArtist = albumArtist; Title = title; Genre = genre; Comment = comment; Rating = rating }
            return! library |> Library.updateBook updatedBook
        }
        
    let updateSingleField (library: Library.Library) (field: string) (value: string) (id: int) : Result<Library.Library, string> =
        let parseRating (s: string) =
            result {
                let! i = Parsers.int s
                return! Rating.create Ok Error i
            }
            
        result {
            do printfn "Updating library field '%s' for entry #%i" field id
            let! book = library |> Library.findById id
            let! update = match field.ToLower() with
                          | "artist" -> Ok (fun (b: Audiobook.Audiobook) -> { b with Artist = Some value })
                          | "album" -> Ok (fun (b: Audiobook.Audiobook) -> { b with Album = Some value})
                          | "albumartist" -> Ok (fun (b: Audiobook.Audiobook) -> { b with AlbumArtist = Some value})
                          | "title" -> Ok (fun (b: Audiobook.Audiobook) -> { b with Title = Some value})
                          | "genre" -> Ok (fun (b: Audiobook.Audiobook) -> { b with Genre = Some value})
                          | "comment" -> Ok (fun (b: Audiobook.Audiobook) -> { b with Comment = Some value})
                          | "rating" ->
                             parseRating value
                             |> Result.map (fun r -> (fun (b: Audiobook.Audiobook) -> { b with Rating = Some r}))
                          | _ -> failwithf "The field '%s' is unknown." field
            let updatedBook = book |> update
            return! library |> Library.updateBook updatedBook
        }
        
    let update (libraryFile: string) (updateConfig: Config.UpdateConfig) =
        result {
            let! library = Library.fromFile libraryFile
            let! updatedLibrary =
                match updateConfig.Fields with
                | [] ->
                    updateConfig.Ids |> List.foldResult (fun accumulator next -> updateInteractively accumulator next) library
                | fields ->
                    let updateField (field, value) library = updateConfig.Ids |> List.foldResult (fun accumulator next -> updateSingleField accumulator field value next) library
                    List.foldResult (fun accumulator next -> updateField next accumulator) library fields
            do! updatedLibrary |> Library.serialize |> IO.writeTextToFile libraryFile
            return 0
        }
        
    let rate (libraryFile: string) (bookId: int option) : Result<int, string> =
            
        let rateSingleBook (a: Audiobook.Audiobook) : Audiobook.Audiobook =
            let input () : Rating.Rating option =
                let mutable breaker = true
                let mutable rating = None
                while breaker do
                    let userInput = Console.ReadLine()
                    if userInput |> String.IsNullOrWhiteSpace then
                        do printfn "You did not enter a rating. The item will be skipped."
                        rating <- None
                        breaker <- false
                    else
                        match b0wter.FSharp.Parsers.parseInt userInput |> Result.fromOption "The input is not a valid number." |> Result.bind (Rating.create Ok Error) with
                        | Ok r ->
                            rating <- Some r
                            breaker <- false
                        | Error e ->
                            do printfn "%s" e
                rating
            
            let x = match (a.Artist, a.Album, a.Title) with
                    | Some artist, Some album, _ -> sprintf "'%s' (%s)" album artist
                    | Some artist, _, Some title -> sprintf "'%s' (%s)" title artist
                    | Some artist, _, _ -> sprintf "'%s' (%s)" (a.Source |> Audiobook.sourceAsString) artist
                    | _, _, _ -> sprintf "'%s'" (a.Source |> Audiobook.sourceAsString)
            let previousRatingHint = match a.Rating with
                                     | Some rating -> sprintf "(the current rating is '%i')" (rating |> Rating.value)
                                     | None -> String.Empty
            do printfn "Please enter a rating from %i-%i for %s %s" Rating.minValue Rating.maxValue x previousRatingHint
            let rating = input ()
            { a with Rating = rating }
            
        result {
            let! library = Library.fromFile libraryFile
            
            let predicate = match bookId with
                            | Some i -> fun (a: Audiobook.Audiobook) -> a.Id = i
                            | None -> fun (a: Audiobook.Audiobook) -> a.Rating = None
                            
            let books = library.Audiobooks |> List.splitBy predicate
            
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
            let libraryFileDirectory = Path.GetDirectoryName(Path.GetFullPath(libraryFile))
            // The `GetFullPath` is required because `Path.Join` may result in paths like this:
            //   /foo/bar/./my_file.mp3
            // which is the same as
            //  /foo/bar/my_file.mp3
            // but it's a string comparison so no logic is applied.
            let allLibraryFiles = library.Audiobooks
                                  |> List.collect Audiobook.allFiles
                                  |> List.map (fun f -> Path.GetFullPath(Path.Join(libraryFileDirectory, f)))
            let! allPathFiles = config.Path |> (IO.listFiles true) |> Result.map IO.filterMp3Files |> Result.map (List.map Path.GetFullPath)
            
            let missingFiles = allPathFiles |> List.except allLibraryFiles
            if missingFiles.IsEmpty then
                do printfn "All files are included in the library."
                return 0
            else
                do missingFiles |> List.iter Console.WriteLine
                return 0
        }
        
    let listFiles libraryFile (config: Config.FilesConfig) =
        result {
            let! library = Library.fromFile libraryFile
            let! book = Library.findById config.Id library
            let files = book |> Audiobook.allFiles |> List.map (fun s -> sprintf "\"%s\"" s)
            let separator = match config.Separator with
                            | Arguments.FileListingSeparator.Space -> " "
                            | Arguments.FileListingSeparator.NewLine -> Environment.NewLine
            return files |> b0wter.FSharp.String.join separator
        }
        
    let write libraryFile (writeConfig: Config.WriteConfig) =
        let bookToMetadata (book: Audiobook.Audiobook) : Metadata.Metadata list =
            let files = book |> Audiobook.allFiles
            files |> List.map (fun file ->
                    Metadata.create file book.Artist book.AlbumArtist book.Album book.Title book.Genre book.Duration book.HasPicture book.Comment
                )
            
        result {
            let! library = Library.fromFile libraryFile
            let metadata = library.Audiobooks |> List.collect bookToMetadata
            do! metadata |> List.map (TagLib.writeMetaData writeConfig) |> List.sequenceResultM |> Result.map ignore
            return 0
        }
        
    [<EntryPoint>]
    let main argv =
        let r = result {
            let errorHandler = ProcessExiter(colorizer = function ErrorCode.HelpText -> None | _ -> None)
            let parser = ArgumentParser.Create<Arguments.MainArgs>(errorHandler = errorHandler)
            let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)
            let config = Config.applyAllArgs results
            
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
                return! completedStatus config.LibraryFile completedConfig.Ids Audiobook.State.Completed
            | Config.NotCompleted notCompletedConfig ->
                return! completedStatus config.LibraryFile notCompletedConfig.Ids Audiobook.State.NotCompleted
            | Config.Aborted abortedConfig ->
                return! completedStatus config.LibraryFile abortedConfig.Ids Audiobook.State.Aborted
            | Config.Files filesConfig ->
                let! string = listFiles config.LibraryFile filesConfig
                do printfn "%s" string
                return 0
            | Config.Unmatched unmatchedConfig ->
                return! unmatched config.LibraryFile unmatchedConfig
            | Config.Uninitialized ->
                do printfn "%s" (parser.PrintUsage())
                return 1
            | Config.Write writeConfig ->
                return! write config.LibraryFile writeConfig
            | Config.Version ->
                do printfn "%s" Version.current
                return 0
        }
        
        match r with
        | Ok statusCode -> statusCode
        | Error e ->
            printfn "%s" e
            1