namespace b0wter.Fbrary

open System
open System.Collections.Generic
open Argu
open b0wter.Fbrary.Arguments
open b0wter.FSharp.Operators
open System.Linq

module Config =
    
    type AddConfig = {
        Path: string
        NonInteractive: bool
        SubDirectoriesAsBooks: bool
        FilesAsBooks: bool
    }
    let private emptyAddConfig = {
        Path = String.Empty
        NonInteractive = false
        SubDirectoriesAsBooks = false
        FilesAsBooks = false
    }
   
    type TableListFormat = {
        Format: string
        MaxColWidth: int
    }
     
    type ListFormat =
        | Cli of format:string
        | Table of TableListFormat
        | Html of input:string * output:string
    
    let private defaultCliFormat = Formatter.CommandLine.defaultFormatString |> ListFormat.Cli
    
    let private defaultTableFormat =
        {
            Format = Formatter.CommandLine.defaultFormatString
            MaxColWidth = 42
        } 
    
    type SortOrder =
        | Ascending
        | Descending
    
    type SortConfig = Audiobook.Audiobook list -> Audiobook.Audiobook list
    type private Sorter = Choice<IEnumerable<Audiobook.Audiobook>, IOrderedEnumerable<Audiobook.Audiobook>> -> IOrderedEnumerable<Audiobook.Audiobook>
    
    let postProcessSortFields (fields: string list) =
        let allowedNames = [ "id"; "artist"; "albumartist"; "album"; "title"; "genre"; "duration"; "rating"; "completed" ]
        let nonMatching = fields |> List.filter (fun f -> allowedNames |> List.contains f |> not)
        if nonMatching.IsEmpty then
            fields
        else
            failwithf "The following fields are unknown: %s" (String.Join(", ", nonMatching))
    
    let applySortField (f: Sorter option) (field: string) =
        let sortOrder = if field.Contains(":d") then SortOrder.Descending else SortOrder.Ascending
        let sorter (books: Choice<IEnumerable<Audiobook.Audiobook>, IOrderedEnumerable<Audiobook.Audiobook>>)
                : Func<Audiobook.Audiobook, 'b> -> IOrderedEnumerable<Audiobook.Audiobook> =
            match books, sortOrder with
            | Choice1Of2 start, Ascending -> start.OrderBy
            | Choice1Of2 start, Descending -> start.OrderByDescending
            | Choice2Of2 middle, Ascending -> middle.ThenBy
            | Choice2Of2 middle, Descending -> middle.ThenByDescending
            
        let noneStringValue = match sortOrder with Ascending -> "ZZZZZZZZZZZZZ" | Descending -> "AAAAAAAAAAAA"

        let composeF (previous: Sorter option) (current: Sorter) : Sorter =
            match previous with
            | None -> current
            | Some f -> f >> Choice2Of2 >> current
        
        match field.ToLower().Replace(":d", "") with
        | "id" ->
            composeF f (fun books -> sorter books (Func<Audiobook.Audiobook, int>(fun b -> b.Id)))
        | "artist" ->
            composeF f (fun books -> sorter books (Func<Audiobook.Audiobook, string>(fun b -> b.Artist |?| noneStringValue)))
        | "albumartist" ->
            composeF f (fun books -> sorter books (Func<Audiobook.Audiobook, string>(fun b -> b.AlbumArtist |?| noneStringValue)))
        | "album" ->
            composeF f (fun books -> sorter books (Func<Audiobook.Audiobook, string>(fun b -> b.Album |?| noneStringValue)))
        | "title" ->
            composeF f (fun books -> sorter books (Func<Audiobook.Audiobook, string>(fun b -> b.Title |?| noneStringValue)))
        | "genre" ->
            composeF f (fun books -> sorter books (Func<Audiobook.Audiobook, string>(fun b -> b.Genre |?| noneStringValue)))
        | "duration" ->
            composeF f (fun books -> sorter books (Func<Audiobook.Audiobook, TimeSpan>(fun b -> b.Duration)))
        | "rating" ->
            composeF f (fun books -> sorter books (Func<Audiobook.Audiobook, int>(fun b -> b.Rating |> Option.map Rating.value |?| 0)))
        | "completed" ->
            composeF f (fun books -> sorter books (Func<Audiobook.Audiobook, Audiobook.State>(fun b -> b.State)))
        | _ -> 
            failwithf "The field '%s' is unknown." field
               
    type ListRatedConfig =
        | Any
        | Rated
        | Unrated
        
    type ListCompletedConfig =
        | Any
        | Completed
        | NotCompleted
        | Aborted
    
    type ListConfig = {
        Filter: string option
        Ids: int list
        Formats: ListFormat list
        Sort: SortConfig 
        Rating: ListRatedConfig
        Completion: ListCompletedConfig
        Completed: bool
    }
    let private emptyListConfig = {
        Filter = None
        Formats = []
        Sort = (fun books -> books |> List.sortBy (fun b -> b.Id))
        Ids = []
        Rating = ListRatedConfig.Any
        Completion = ListCompletedConfig.Any
        Completed = false
    }
    
    type RemoveConfig = {
        Id: int
    }
    
    type UpdateConfig = {
        Ids: int list
        Fields: (string * string) list
    }
    let private emptyUpdateConfig = {
        Ids = []
        Fields = []
    }
    
    type RateConfig = {
        Id: int option
    }
    
    type CompletedConfig = {
        Ids: int list
    }
    
    type NotCompletedConfig = {
        Ids: int list
    }
    
    type AbortedConfig = {
        Ids: int list
    }
    
    type FilesConfig = {
        Ids: int list
        Separator: FileListingSeparator
        ListMissing: bool
    }
    
    let private emptyFilesConfig = {
        Ids = []
        Separator = NewLine
        ListMissing = false
    }
    
    type UnmatchedConfig = {
        Path: string
    }
    
    type WriteConfig = {
        Fields: string list
        DryRun: bool
        NonInteractive: bool
    }
    let private emptyWriteConfig = {
        Fields = []
        DryRun = false
        NonInteractive = false
    }
    
    type DetailsConfig = {
        Ids: int list
    }
    
    type Command
        = Add of AddConfig
        | List of ListConfig
        | Remove of RemoveConfig
        | Update of UpdateConfig
        | Rate of RateConfig
        | Completed of CompletedConfig
        | NotCompleted of NotCompletedConfig
        | Aborted of AbortedConfig
        | Unmatched of UnmatchedConfig
        | Files of FilesConfig
        | Uninitialized
        | Write of WriteConfig
        | Migrate
        | Details of DetailsConfig
        | Version
    
    type Config = {
        Command: Command
        Verbose: bool
        LibraryFile: string
    }
    let private empty = { Command = Uninitialized; Verbose = false; LibraryFile = String.Empty }
    
    let applyListArg (config: ListConfig) (l: ListArgs) : ListConfig =
        match l with
        | ListArgs.Cli format ->
            let rec step accumulator remaining =
                match remaining with
                | [] ->
                    (ListFormat.Cli format) :: accumulator
                | (ListFormat.Cli _) :: tail ->
                    accumulator @ (ListFormat.Cli format) :: tail
                | otherFormat :: tail ->
                    step (otherFormat :: accumulator) tail
            let updatedFormats = step [] config.Formats
            { config with Formats = updatedFormats }
        | ListArgs.Table format ->
            let rec step accumulator remaining =
                match remaining with
                | [] ->
                    (ListFormat.Table { defaultTableFormat with Format = format }) :: accumulator
                | (ListFormat.Table tableFormat) :: tail ->
                    accumulator @ ((ListFormat.Table { tableFormat with Format = format }) :: tail)
                | otherFormat :: tail ->
                    step (otherFormat :: accumulator) tail
            let updatedFormats = step [] config.Formats
            { config with Formats = updatedFormats }
        | ListArgs.Html (input, output) ->
            let rec step accumulator remaining =
                match remaining with
                | [] ->
                    (ListFormat.Html (input, output)) :: accumulator
                | (ListFormat.Html _) :: tail ->
                    accumulator @ (ListFormat.Html (input, output)) :: tail
                | otherFormat :: tail ->
                    step (otherFormat :: accumulator) tail
            let updatedFormats = step [] config.Formats
            { config with Formats = updatedFormats }
        | ListArgs.MaxTableColumnWidth width ->
            let formats = config.Formats
                          |> List.map (function | ListFormat.Table f -> ListFormat.Table { f with MaxColWidth = width }
                                                | ListFormat.Cli   f -> ListFormat.Cli f
                                                | ListFormat.Html  (i, o) -> ListFormat.Html (i, o))
            { config with Formats = formats }
        | Filter filter -> { config with Filter = Some filter }
        | ListArgs.Ids ids -> { config with Ids = ids }
        | ListArgs.NotCompleted ->
            if config.Completion <> ListCompletedConfig.Any then
                do printfn "You have defined more than one completion parameter. Only the last will be used. Current: %A, new: %A" config.Completion ListCompletedConfig.NotCompleted
            else ()
            { config with Completion = ListCompletedConfig.NotCompleted }
        | ListArgs.Completed ->
            if config.Completion <> ListCompletedConfig.Any then
                do printfn "You have defined more than one completion parameter. Only the last will be used. Current: %A, new: %A" config.Completion ListCompletedConfig.Completed
            else ()
            { config with Completion = ListCompletedConfig.Completed }
        | ListArgs.Aborted ->
            if config.Completion <> ListCompletedConfig.Any then
                do printfn "You have defined more than one completion parameter. Only the last will be used. Current: %A, new: %A" config.Completion ListCompletedConfig.Aborted
            else ()
            { config with Completion = ListCompletedConfig.Aborted }
        | ListArgs.Rated ->
            if config.Rating <> ListRatedConfig.Any then
                do printfn "You have defined more than one rating parameter. Only the last will be used. Current: %A, new %A" config.Rating ListRatedConfig.Rated
            else ()
            { config with Rating = ListRatedConfig.Rated }
        | ListArgs.Unrated ->
            if config.Rating <> ListRatedConfig.Any then
                do printfn "You have defined more than one rating parameter. Only the last will be used. Current: %A, new %A" config.Rating ListRatedConfig.Unrated
            else ()
            { config with Rating = ListRatedConfig.Unrated }
        | Sort fields ->
            let linqSorter = fields |> List.fold (fun (accumulator: Sorter option) next -> Some (applySortField accumulator next)) None
            let sorter = match linqSorter with
                         | None -> id
                         | Some s ->
                             fun (books: Audiobook.Audiobook list) ->
                                 let b = Choice<IEnumerable<Audiobook.Audiobook>, IOrderedEnumerable<Audiobook.Audiobook>>.Choice1Of2 books
                                 s b |> List.ofSeq
            { config with Sort = sorter } 
        
    let applyAddArg (config: AddConfig) (a: AddArgs) : AddConfig =
        match a with
        | Path p -> { config with Path = p }
        | AddArgs.NonInteractive -> { config with NonInteractive = true }
        | SubDirectoriesAsBooks -> { config with SubDirectoriesAsBooks = true }
        | FilesAsBooks -> { config with FilesAsBooks = true }
        
    let applyFilesArg (config: FilesConfig) (f: FilesArgs) : FilesConfig =
        match f with
        | Ids ids -> { config with Ids = ids }
        | Separator s -> { config with Separator = s }
        | Missing -> { config with ListMissing = true }
        
    let applyWriteArg (config: WriteConfig) (w: WriteArgs) : WriteConfig =
        match w with
        | Fields fields -> { config with Fields = fields }
        | DryRun -> { config with DryRun = true }
        | NonInteractive -> { config with NonInteractive = true }
        
    let applyUpdateArg (config: UpdateConfig) (u: UpdateArgs) : UpdateConfig =
        match u with
        | UpdateArgs.Ids id -> { config with Ids = id }
        | UpdateArgs.Field (field, value) -> { config with Fields = (field, value) :: config.Fields }
    
    // Define functions that take arguments and apply them to a config.
    // Use this to fold the configuration from the arguments.
    let applyMainArg (config: Config) (m: MainArgs) : Config =
        match m with
        | Verbose ->
            { config with Verbose = true }
        | Library l ->
            { config with LibraryFile = l }
        | MainArgs.Remove id ->
            { config with Command = Remove { Id = id } }
        | MainArgs.Update update ->
            let updateConfig = match config.Command with
                               | Update u -> u
                               | _ -> emptyUpdateConfig
            let updatedUpdateConfig = update.GetAllResults() |> List.fold applyUpdateArg updateConfig
            { config with Command = Update updatedUpdateConfig }
        | MainArgs.Add add ->
            let addConfig = match config.Command with
                            | Add a -> a
                            | _ -> emptyAddConfig
            let updatedAddConfig = add.GetAllResults() |> List.fold applyAddArg addConfig
            { config with Command = Add updatedAddConfig }
        | MainArgs.Rate id ->
            { config with Command = Rate { Id = id } }
        | MainArgs.Completed ids ->
            { config with Command = Completed { Ids = ids } }
        | MainArgs.NotCompleted ids ->
            { config with Command = NotCompleted { Ids = ids } }
        | MainArgs.Aborted ids ->
            { config with Command = Aborted { Ids = ids } }
        | MainArgs.Files files ->
            let filesConfig = match config.Command with
                              | Files f -> f
                              | _ -> emptyFilesConfig
            let updatedFilesConfig = files.GetAllResults() |> List.fold applyFilesArg filesConfig
            { config with Command = Files updatedFilesConfig  }
        | MainArgs.Unmatched path ->
            { config with Command = Unmatched { Path = path } }
        | MainArgs.List l ->
            let listConfig = match config.Command with
                             | List c -> c
                             | _ -> emptyListConfig
            let updatedListConfig = l.GetAllResults() |> List.fold applyListArg listConfig
            let formatters = if updatedListConfig.Formats.IsEmpty then [ ListFormat.Cli Formatter.CommandLine.defaultFormatString ]
                             else updatedListConfig.Formats |> List.rev
            let updatedListConfig = { updatedListConfig with Formats = formatters }
            { config with Command = List updatedListConfig }
        | MainArgs.Write w ->
            let writeConfig = match config.Command with
                              | Write w -> w
                              | _ -> emptyWriteConfig
            let updatedWriteConfig = w.GetAllResults() |> List.fold applyWriteArg writeConfig
            { config with Command = Write updatedWriteConfig }
        | MainArgs.Migrate ->
            { config with Command = Migrate }
        | MainArgs.Details ids ->
            { config with Command = Details { Ids = ids } }
        | MainArgs.Version ->
            { config with Command = Version }
            
    let applyAllArgs (results: ParseResults<MainArgs>) =
        //
        // This is hacky but makes sure that argu checks that the field names for the sort parameter are valid.
        // Is only possible in this function because a reference of the `ParseResults<...>` is required.
        // All subsequent functions work on the extracted results.
        //
        let _ = results.TryGetResult(<@ MainArgs.List @>) |> Option.map (fun r -> r.TryPostProcessResult(<@ ListArgs.Sort @>, postProcessSortFields))
        
        //
        // This is rather hacky but it makes sure that Argu checks the library parameter
        // if the command is not the version command.
        // Limitation of argu or my knowledge :)
        //
        let args = results.GetAllResults ()
        if args |> List.exists (function MainArgs.Version -> true | _ -> false) then
            { empty with Command = Version }
        else
            let _ = results.GetResult(<@ MainArgs.Library @>)
            args |> List.fold applyMainArg empty
            
