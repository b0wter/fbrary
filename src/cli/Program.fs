namespace b0wter.Fbrary

open Argu
open System
open FsToolkit.ErrorHandling
open b0wter.FSharp.Operators
open b0wter.Fbrary
open b0wter.Fbrary.Utilities
 
module Program =
        
    let getDirectoryFromArgs (argv: string []) =
        if argv.Length = 1 then Ok argv.[0]
        else Error "This program takes exactly one argument, the directory to recursively scan for mp3 files."
        
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
            let files = files |> List.map IO.filterMp3Files |> List.skipEmpty
            return! files |> List.map createBook |> List.sequenceResultM
        }
        
    let addFile interactive (path: string) (idGenerator: unit -> int) : Result<Audiobook.Audiobook, string> =
        result {
            let! filename = IO.getIfMp3File path
            let! metadata = filename |> TagLib.readMetaData
            return Audiobook.createFromSingle interactive idGenerator metadata
        }
        
    let addPath (addConfig: Config.AddConfig) (path: string) (idGenerator: unit -> int) : Result<Audiobook.Audiobook list, string> =
        if IO.File.exists(path) then
            if addConfig.SubDirectoriesAsBooks then printfn "The option to add subdirectories as books is ignored if the given path points to a file."
            elif addConfig.FilesAsBooks then printfn "The option to add files as independent books is ignored if the given path points to a file."
            else ()
            addFile (not <| addConfig.NonInteractive) path idGenerator |> Result.map List.singleton
        elif IO.Directory.exists(path) then
            addDirectory addConfig path idGenerator
        else
            Error <| sprintf "The given path '%s' does not exist." path
            
    // TODO: consolidate all the different ways to read/create a library        
    
    let private readLibraryOr (ifNotExisting: unit -> Library.Library) (filename: string) : Result<Library.Library, string> =
        if IO.File.exists(filename) then
            (IO.readTextFromFile filename) |> Result.bind Library.deserialize
        elif IO.Directory.exists(filename) then
            Error "The given path is a directory not a file."
        else
            ifNotExisting () |> Ok
            
    let private readLibraryOrError filename = 
        if IO.File.exists(filename) then
            (IO.readTextFromFile filename) |> Result.bind Library.deserialize
        elif IO.Directory.exists(filename) then
            Error "The given path is a directory not a file."
        else
            Error (sprintf "The library file '%s' does not exist." filename)
        
    let readOrCreateLibrary = 
        readLibraryOr (fun () -> Library.empty)
        
    let readLibraryIfExisting (filename: string) : Result<Library.Library option, string> =
        if IO.File.exists(filename) then
            (IO.readTextFromFile filename) |> Result.bind (Library.deserialize >> Result.map Some)
        elif IO.Directory.exists(filename) then
            Error "The given path is a directory not a file."
        else
            Ok None

    let formatAudiobook (format: Config.ListFormat) (sort: Config.SortConfig) (books: Audiobook.Audiobook list) : string list =
        let r = 
            result {
                let sortedBooks = books |> sort 
                match format with
                    | Config.ListFormat.Cli f ->
                        return sortedBooks |> List.map (Formatter.CommandLine.applyAll f)
                    | Config.ListFormat.Table table ->
                        return sortedBooks |> Formatter.Table.apply table.MaxColWidth table.Format
                    | Config.ListFormat.Html (template, output) ->
                        let! template = IO.readTextFromFile template
                        let result = sortedBooks |> Formatter.Html.apply template
                        do! IO.writeTextToFile output result
                        return []
            }
        match r with
        | Ok o -> o
        | Error e -> [ e ]
            
    let idGenerator (library: Library.Library) : (unit -> int) =
        let mutable counter = if library.Audiobooks.IsEmpty then 0
                              else  library.Audiobooks |> List.map (fun a -> a.Id) |> List.max
        fun () ->
            do counter <- (counter + 1)
            counter
            
    let add (addConfig: Config.AddConfig) (library: Library.Library) : Result<Library.Library, string> =
        result {
            let idGenerator = idGenerator library
            let! audiobooks = addPath addConfig addConfig.Path idGenerator
            
            let folder = fun (lib: Result<Library.Library, string>) (book: Audiobook.Audiobook) ->
                lib |> Result.bind (Library.addBook book)
                
            return! audiobooks |> List.fold folder (Ok library)
        }
            
    let remove (removeConfig: Config.RemoveConfig) (library: Library.Library) : Result<Library.Library, string> =
        result {
            let! audiobook = library |> Library.findById removeConfig.Id
            let updatedLibrary = library |> Library.removeBook audiobook
            // TODO: remove `if`
            if updatedLibrary.Audiobooks.Length = library.Audiobooks.Length then
                do printfn "No audio book has the id '%i'. The library has not been modified." removeConfig.Id
            else ()
            return updatedLibrary
        }
        
    let list (listConfig: Config.ListConfig) (library: Library.Library) : Result<unit, string> =
        
        // Should the predicates be defined while parsing the cli arguments?
        
        let ratingPredicate =
            match listConfig.Rating with
            | Config.ListRatedConfig.Any -> None
            | Config.ListRatedConfig.Rated -> Some (fun (b: Audiobook.Audiobook) -> (b |> Audiobook.rating).IsSome)
            | Config.ListRatedConfig.Unrated -> Some (fun (b: Audiobook.Audiobook) -> (b |> Audiobook.rating).IsNone)
            
        let completionPredicate =
            match listConfig.Completion with
            | Config.ListCompletedConfig.Any -> None
            | Config.ListCompletedConfig.Aborted -> Some (fun (b: Audiobook.Audiobook) -> (b |> Audiobook.state) = Audiobook.State.Aborted)
            | Config.ListCompletedConfig.Completed -> Some (fun (b: Audiobook.Audiobook) -> (b |> Audiobook.state) = Audiobook.State.Completed)
            | Config.ListCompletedConfig.NotCompleted -> Some (fun (b: Audiobook.Audiobook) -> (b |> Audiobook.state) = Audiobook.State.NotCompleted)
        
        let filterPredicate =
            match listConfig.Filter with
            | Some s when String.IsNullOrWhiteSpace(s) -> None
            | None -> None
            | Some s -> Some (fun (b: Audiobook.Audiobook) -> b |> Audiobook.containsStringCaseInsensitive (s.ToLower()))
            
        let predicates = [ ratingPredicate; completionPredicate; filterPredicate ]
                         |> List.choose id
                         
        let combinedPredicate a = List.forall (fun p -> a |> p) predicates
        
        result {
            let filtered = library.Audiobooks |> List.filter combinedPredicate
            do if filtered.IsEmpty then do printfn "Found no matching audio books."
               else do
                   listConfig.Formats
                   |> List.collect (fun format -> formatAudiobook format listConfig.Sort filtered)
                   |> List.iter Console.WriteLine
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
                          | _ -> Error (sprintf "The field '%s' is unknown." field)
            let updatedBook = book |> update
            return! library |> Library.updateBook updatedBook
        }
        
    let update (updateConfig: Config.UpdateConfig) (library: Library.Library) =
        result {
            let! updatedLibrary =
                match updateConfig.Fields with
                | [] ->
                    updateConfig.Ids |> List.foldResult updateInteractively library
                | fields ->
                    let updateField (field, value) library = updateConfig.Ids |> List.foldResult (fun accumulator -> updateSingleField accumulator field value) library
                    List.foldResult (fun accumulator next -> updateField next accumulator) library fields
            return updatedLibrary
        }
        
    let rate (rateConfig: Config.RateConfig) (library: Library.Library) : Result<Library.Library, string> =
            
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
            let predicate = match rateConfig.Id with
                            | Some i -> fun (a: Audiobook.Audiobook) -> a.Id = i
                            | None -> fun (a: Audiobook.Audiobook) -> a.Rating = None
                            
            let books = library.Audiobooks |> List.splitBy predicate
            
            if books.Matching.Length > 0 then
                let updatedBooks = books.Matching |> List.map rateSingleBook
                let mergedBooks = (updatedBooks @ books.NonMatching) |> List.sortBy (fun a -> a.Id)
                
                let updatedLibrary = { library with Audiobooks = mergedBooks }
                return updatedLibrary
            else
                let text = match rateConfig.Id with
                           | Some i -> sprintf "There is no book with the id '%i'." i
                           | None -> sprintf "All books have ratings. If you want to change a rating please specify a book id."
                do printfn "%s" text
                return library
        }
        
    let completedStatus ids status library = 
        result {
            let! books = Library.findByIds ids library
            let updatedBooks = books |> List.map (Audiobook.withCompletionStatus status)
            return! Library.updateBooks updatedBooks library
        }
        
    let unmatched (config: Config.UnmatchedConfig) (library: Library.Library) =
        result {
            let allLibraryFiles = library.Audiobooks
                                  |> List.collect Audiobook.allFiles
                                  |> List.map (IO.Path.fullPath >> IO.simplifyPath) 
            let! allPathFiles = config.Path |> (IO.listFiles true) |> Result.map IO.filterMp3Files |> Result.map (List.map IO.Path.fullPath)
            let missingFiles = allPathFiles |> List.except allLibraryFiles
            if missingFiles.IsEmpty then
                do printfn "All files are included in the library."
            else
                do missingFiles |> List.iter Console.WriteLine
        }
        
    let listFiles libraryFile (config: Config.FilesConfig) =
        result {
            let! library = Library.fromFile libraryFile
            let! books = if config.Ids.IsEmpty then
                            library.Audiobooks |> Ok
                         else
                            config.Ids |> List.traverseResultA (fun id -> Library.findById id library) |> Result.mapError (b0wter.FSharp.String.join "; ")
            
            let filter = match config.ListMissing with
                         | false -> id
                         | true -> List.filter IO.fileDoesNotExist
            let files = books |> List.collect (fun book -> book |> Audiobook.allFiles |> filter |> List.map (sprintf "\"%s\""))
            
            let separator = match config.Separator with
                            | Arguments.FileListingSeparator.Space -> " "
                            | Arguments.FileListingSeparator.NewLine -> Environment.NewLine
            return files |> b0wter.FSharp.String.join separator
        }
        
    let write (writeConfig: Config.WriteConfig) (library: Library.Library) =
        let bookToMetadata (book: Audiobook.Audiobook) : Metadata.Metadata list =
            let files = book |> Audiobook.allFiles
            files |> List.map (fun file ->
                    Metadata.create file book.Artist book.AlbumArtist book.Album book.Title book.Genre book.Duration book.HasPicture book.Comment
                )
            
        result {
            let metadata = library.Audiobooks |> List.collect bookToMetadata
            do! metadata |> List.map (TagLib.writeMetaData writeConfig) |> List.sequenceResultM |> Result.map ignore
        }
        
    let listDetails (config: Config.DetailsConfig) (library: Library.Library) =
        result {
            let! books = library |> Library.findByIds config.Ids
            do books |> List.iter (Audiobook.details >> List.iter Console.WriteLine)
        }
        
    let migrateLibrary (libraryFile: string) _ =
        result {
            // Make sure the config no longer uses the `LastScanned` field.
            do printfn "Making sure the `LastScanned` field is replaced with `LastUpdated`."
            let! content = IO.readTextFromFile libraryFile
            let updatedContent = content.Replace("\"LastScanned\": \"", "\"LastUpdated\": \"")
            let! library = Library.deserialize updatedContent
            let updatedLibrary = { library with LastUpdated = DateTime.Now }
            
            // Update paths in the config to be absolute.
            do printfn "Making sure the sources use absolute paths."
            let rootPath = IO.Path.directoryName libraryFile
            let rootPath = IO.Path.combine(Environment.CurrentDirectory, rootPath)
            let updateSource (book: Audiobook.Audiobook) : Audiobook.Audiobook =
                do printfn $"Migrating relative path to use the root '%s{rootPath}'."
                let combinator (rootPath: string) (filename: string) =
                    if filename.StartsWith(rootPath) then
                        do printfn $"Skipping '%s{filename}' because it already starts with the root path."
                        filename
                    else
                        let updated = IO.Path.combine(rootPath, filename)
                        do printfn $"Updated '%s{filename}' to '%s{updated}'."
                        updated
                    
                let configuredCombinator = combinator rootPath
                let updatedSource = match book.Source with
                                    | Audiobook.AudiobookSource.SingleFile f ->
                                        configuredCombinator f |> Audiobook.AudiobookSource.SingleFile
                                    | Audiobook.AudiobookSource.MultiFile m ->
                                        m |> List.map configuredCombinator |> Audiobook.AudiobookSource.MultiFile 
                { book with Source = updatedSource }
            let updatedFilesLibrary = { library with Audiobooks = updatedLibrary.Audiobooks |> List.map updateSource }
            do! updatedFilesLibrary |> Library.serialize |> IO.writeTextToFile libraryFile
        }
        
    let move (config: Config.MoveConfig) (library: Library.Library) =
        let moveSingleFile source target =
            result {
                let targetFilename =
                    if target |> IO.File.hasExtension then target
                    else IO.Path.combine (target, (source |> IO.File.fileName))
                let targetFolderName = targetFilename |> IO.Directory.directoryName
                do! IO.Directory.create targetFolderName
                do! IO.File.move source targetFilename
            }
            
        /// Moves all source files to the target.
        /// Checks whether the target is an existing file. If it is the process is aborted.
        /// Makes sure that the target directory exists.
        let moveMultipleFiles (sources: string list) target =
            result {
                if target |> IO.File.exists then
                    return! Error "Cannot move an audio book consisting of multiple files to a file location."
                else
                    do printfn "test"
                    let basePath = IO.findLargestCommonPath sources
                    return! sources |> List.traverseResultM (fun s ->
                        result {
                            // remove the SeparatorChar because it confuses `Path.combine` because it thinks
                            // relative path is an absolute path otherwise
                            let relativePath = s.Substring(basePath.Length).TrimStart(IO.Path.DirectorySeparatorChar)
                            let targetFilename = IO.Path.combine (target, relativePath)
                            return! moveSingleFile s targetFilename
                        }) |> Result.map (fun _ -> ())
            }
       
        let move (source: Audiobook.AudiobookSource) target =
            result {
                return! match source with
                        | Audiobook.AudiobookSource.SingleFile file -> moveSingleFile file target
                        | Audiobook.AudiobookSource.MultiFile files -> moveMultipleFiles files target
            }
            
        result {
            let! book = library |> Library.findById config.Id
            do! move book.Source config.Target
            return ()
        }
        
    let identify (config: Config.IdConfig) (library: Library.Library) =
        let shortName (b: Audiobook.Audiobook) =
            match b.Album, b.Title with
            | Some album, Some title -> sprintf " (%s, %s)" album title
            | Some album, None -> sprintf " (%s)" album
            | None, Some title -> sprintf " (%s)" title
            | None, None -> String.Empty
            
        let printBook (b: Audiobook.Audiobook) =
           let filename = match b.Source with
                          | Audiobook.AudiobookSource.SingleFile f ->
                              f
                          | Audiobook.AudiobookSource.MultiFile many ->
                              many
                              |> List.tryHead
                              |> Option.map (fun s -> sprintf "%s + %i more" s (many.Length - 1))
                              |?| "<no file>"
           printfn "%i%s (%s)" b.Id (b |> shortName) filename
            
        match library.Audiobooks |> List.filter (Audiobook.containsPath config.Target) with
        | [] ->
            do printfn "The file is not part of any audio book."
        | list ->
            do list |> List.iter printBook
        
    let private parseCommandLineArguments argv =
        let errorHandler = ProcessExiter(colorizer = function ErrorCode.HelpText -> None | _ -> None)
        let parser = ArgumentParser.Create<Arguments.MainArgs>(errorHandler = errorHandler)
        (parser, parser.ParseCommandLine(inputs = argv, raiseOnUsage = true))
        
    let runOnExistingAndSave libraryFile (f: Library.Library -> Result<Library.Library, string>) =
        result {
            let! library = readLibraryOrError libraryFile
            let! updatedLibrary = f library 
            return! updatedLibrary |> Library.serialize |> IO.writeTextToFile libraryFile
        }
        
    let runOnExistingOrNewAndSave libraryFile (f: Library.Library -> Result<Library.Library, string>) =
        result {
            let! library = readOrCreateLibrary libraryFile
            let! updatedLibrary = f library 
            return! updatedLibrary |> Library.serialize |> IO.writeTextToFile libraryFile
        }
        
    let runOnExisting libraryFile (f: Library.Library -> Result<'a, string>) =
        result {
            let! library = readOrCreateLibrary libraryFile
            return! f library
        }
        
    [<EntryPoint>]
    let main argv =
        
        let r = result {
            let parser, results = parseCommandLineArguments argv
            let config = Config.applyAllArgs results
            
            let runOnExistingAndSave = runOnExistingAndSave config.LibraryFile
            let runOnExistingOrNewAndSave = runOnExistingOrNewAndSave config.LibraryFile
            let runOnExisting = runOnExisting config.LibraryFile
            
            match config.Command with
            | Config.Add addConfig ->
                return! (runOnExistingOrNewAndSave (add addConfig))
            | Config.List listConfig ->
                return! (runOnExisting (list listConfig))
            | Config.Remove removeConfig ->
                return! (runOnExistingAndSave (remove removeConfig))
            | Config.Update updateConfig ->
                return! (runOnExistingAndSave (update updateConfig))
            | Config.Rate rateConfig ->
                return! (runOnExistingAndSave (rate rateConfig))
            | Config.Command.Completed completedConfig ->
                return! (runOnExistingAndSave (completedStatus completedConfig.Ids Audiobook.State.Completed))
            | Config.Command.NotCompleted notCompletedConfig ->
                return! (runOnExistingAndSave (completedStatus notCompletedConfig.Ids Audiobook.State.NotCompleted))
            | Config.Command.Aborted abortedConfig ->
                return! (runOnExistingAndSave (completedStatus abortedConfig.Ids Audiobook.State.Aborted))
            | Config.Files filesConfig ->
                let! string = listFiles config.LibraryFile filesConfig
                if String.IsNullOrWhiteSpace(string) then ()
                else do Console.WriteLine(string)
            | Config.Unmatched unmatchedConfig ->
                return! (runOnExisting (unmatched unmatchedConfig))
            | Config.Uninitialized ->
                do Console.WriteLine(parser.PrintUsage())
            | Config.Write writeConfig ->
                return! (runOnExisting (write writeConfig))
            | Config.Migrate ->
                return! (runOnExisting (migrateLibrary config.LibraryFile))
            | Config.Details detailsConfig ->
                return! (runOnExisting (listDetails detailsConfig))
            | Config.Move moveConfig ->
                return! (runOnExisting (move moveConfig))
            | Config.Id idConfig ->
                return! (runOnExisting ((identify idConfig) >> Ok))
            | Config.Version ->
                do Console.WriteLine(Version.current)
        }
        
        match r with
        | Ok () -> 0
        | Error e ->
            printfn "%s" e
            1