namespace b0wter.Fbrary

open b0wter.Fbrary.Core

module Formatter =
    open b0wter.Fbrary.Assets
    open System.Text.RegularExpressions
    
    // If you change the regex you probably need to change `optionalSymbol` as well!
    let optionalElementsRegex = Regex("(\?\?)[^\?]*(\?\?)") 
    let optionalSymbol = "??" // If you change the symbol you need to change the regex as well!
    let optionalSymbolLength = optionalSymbol.Length
    
    // If you change the regex you probably need to change `replaceSymbol` as well!
    let elementRegex = Regex("(%)[^\%]*(%)") 
    let replaceSymbol = "%" // If you change the symbol you need to change the regex as well!
    let replaceSymbolLength = replaceSymbol.Length
    
    let durationFormat = "h\:mm"
    
    module CommandLine =
        
        let defaultFormatString = "(%id%) %album% ??(%artist%) ??%duration%"
        
        /// Tries to replace a single format identifier (%%..%%) with a proper value.
        /// `target` is a simple format identifier like %%artist%%
        let applyToSingleFormatString (ifNotFound: string -> Audiobook.Audiobook -> string) (breakIfNotFound: bool) (target: string) (a: Audiobook.Audiobook) =
            let rec step (accumulator: string) (remainingFields: Fields.Field list) =
                match remainingFields with
                | [] -> Some accumulator
                | field :: tail ->
                    if field.Cli = target then
                        match a |> field.Replacer with
                        | Some text ->
                            let newAccumulator = accumulator.Replace(field.Cli, text)
                            step newAccumulator tail
                        | None when breakIfNotFound ->
                            None
                        | None ->
                            let newAccumulator = ifNotFound field.Cli a
                            step newAccumulator tail
                    else
                        step accumulator tail
            step target Fields.allFields
            
        /// Replace all format identifiers inside an optional format identifier (??...??) with proper values.
        /// If the given book returns None for any identifier None is returned.
        let applySingleOptional (optionalMatch: string) (a: Audiobook.Audiobook) =
            if optionalMatch.StartsWith(optionalSymbol) && optionalMatch.EndsWith(optionalSymbol) then
                let optionalMatch = optionalMatch.Substring(optionalSymbolLength, optionalMatch.Length - 2 * optionalSymbolLength)
                let elements = elementRegex.Matches(optionalMatch) |> Seq.map (fun m -> m.Value) |> List.ofSeq
                let applyToSingle = applyToSingleFormatString (fun _ _ -> System.String.Empty) true
                
                let rec step (accumulator: string) (remainingElements: string list) : string option =
                    match remainingElements with
                    | [] -> Some accumulator
                    | head :: tail ->
                        match applyToSingle head a with
                        | Some result ->
                            step (accumulator.Replace(head, result)) tail
                        | None ->
                            None
                
                step optionalMatch elements
            else
                // In case the input is not of the form '?? ... ??' it is not a valid optional formatting.
                Some optionalMatch
        
        /// Finds all optional format identifiers and replaces them with proper values.
        /// Calls `applySingleOption` to do so.
        let applyAllOptional (a: Audiobook.Audiobook) (format: string) =
            let elements = optionalElementsRegex.Matches(format) |> Seq.map (fun m -> m.Value)
            Seq.fold (fun (state: string) (next: string) ->
                    match applySingleOptional next a with
                    | Some text -> state.Replace(next, text)
                    | None -> state.Replace(next, System.String.Empty)
                ) format elements
            
        let applyAllSimple (a: Audiobook.Audiobook) (format: string) =
            let elements = elementRegex.Matches(format) |> Seq.map (fun m -> m.Value)
            let ifNotFound = fun (identifier: string) _ -> sprintf "<no %s set>" (identifier.Substring(replaceSymbolLength, identifier.Length  - 2 * replaceSymbolLength))
            Seq.fold (fun (state: string) (next: string) ->
                    match applyToSingleFormatString ifNotFound false next a with
                    | Some text -> state.Replace(next, text)
                    | None -> System.String.Empty
                ) format elements
            
        let applyAll (format: string) (a: Audiobook.Audiobook) =
            // Apply all optional format strings because they include simple format strings.
             format |> (applyAllOptional a)
                    |> (applyAllSimple a)
            
        
    module Table =
        open System
        open b0wter.FSharp
            
        type Cell = {
            Content: string
            Width: int
        }
        
        type Column = {
            /// Width is only the content of the column. Does not include the separator char and the whitespace padding.
            Width: int
            Header: string
            Cells: Cell list
        }
        
        type Row = {
            Index: int
            Cells: Cell list
        }
        
        let columnsToRows (columns: Column list) =
            // first index is column, second is row
            let array = columns |> Array.ofList |> Array.map (fun (c: Column) -> c.Cells |> Array.ofList)
            let rowCount = array.[0].Length
            let selectRow (row: int) (array: 'a [] []) : 'a [] =
                array |> Array.map (fun a -> a.[row])
            seq {
                for i in [0..rowCount - 1] do
                    yield { Index = i; Cells = array |> selectRow i |> List.ofArray }
            } |> List.ofSeq
                
        let line leftBorder columnBorder rightBorder borderPaddingCharacter contentPaddingCharacter (cells: (string * int) list) =
            let rec fold (accumulator: string) (remaining: (string * int) list) (isFirst: bool) =
                match remaining with
                | [] ->
                    accumulator
                | [ (content, width) ] when isFirst ->
                    // only one cell
                    sprintf "%c%c%s%c%c" leftBorder borderPaddingCharacter (content.PadRight(width, contentPaddingCharacter)) borderPaddingCharacter rightBorder
                | [ (content, width) ] ->
                    // last cell
                    let newPart = (sprintf "%c%s%c%c" borderPaddingCharacter (content.PadRight(width, contentPaddingCharacter)) borderPaddingCharacter rightBorder)
                    accumulator + newPart
                | (content, width) :: tail when isFirst ->
                    let newPart = sprintf "%c%c%s%c%c" leftBorder borderPaddingCharacter (content.PadRight(width, contentPaddingCharacter)) borderPaddingCharacter columnBorder
                    fold (accumulator + newPart) tail false
                | (content, width) :: tail ->
                    let newPart = sprintf "%c%s%c%c" borderPaddingCharacter (content.PadRight(width, contentPaddingCharacter)) borderPaddingCharacter columnBorder
                    fold (accumulator + newPart) tail false
            fold String.Empty cells true
            
        let createHeader (columns: Column list) =
            let boxContent = columns |> List.map (fun c -> ("", c.Width))
            let textContent = columns |> List.map (fun c -> (c.Header, c.Width))
            [
                line HighlightBorder.cUpperLeft HighlightConnectors.doubleHTUpper HighlightBorder.cUpperRight HighlightBorder.hLine HighlightBorder.hLine boxContent
                line HighlightBorder.vLine SimpleBorder.vLine HighlightBorder.vLine ' ' ' ' textContent
                line HighlightConnectors.doubleVTLeft SimpleBorder.center HighlightConnectors.doubleVTRight SimpleBorder.hLine SimpleBorder.hLine boxContent
            ]
                
        let createRow (row: Row)=
            let cutText (maxLength: int) (s: string) : string =
                if s.Length > maxLength then
                    s.Substring(0 ,maxLength - 3) + "..."
                else
                    s
            let textContent = row.Cells |> List.map (fun r -> (r.Content |> (cutText r.Width), r.Width))
            line HighlightBorder.vLine SimpleBorder.vLine HighlightBorder.vLine ' ' ' ' textContent
            
        let createFooter (columns: Column list) =
            let contentWidth = (columns |> List.sumBy (fun c -> c.Width + 3)) - 3
            let totalRows = if columns.Length > 0 then columns.[0].Cells.Length else 0
            let boxContent = columns |> List.map (fun c -> ("", c.Width))
            
            let fullSummary = sprintf "Total: %i" totalRows
            let shortSummary = sprintf "%i" totalRows
            let noSummary = "*"
            
            let fullWidthContent = [ "", contentWidth ]
            
            let summary = if contentWidth >= fullSummary.Length then fullSummary
                          elif contentWidth >= shortSummary.Length then shortSummary
                          else noSummary
                          
            let textContent = [ summary, contentWidth ]
            [
                line HighlightConnectors.doubleVTLeft SimpleBorder.tLower HighlightConnectors.doubleVTRight SimpleBorder.hLine SimpleBorder.hLine boxContent
                line HighlightBorder.vLine ' ' HighlightBorder.vLine ' ' ' ' textContent
                line HighlightBorder.cLowerLeft HighlightConnectors.doubleHTLower HighlightBorder.cLowerRight HighlightBorder.hLine HighlightBorder.hLine fullWidthContent
            ]
                
        let createColumns maxColumnWidth (identifiers: string list) (books: Audiobook.Audiobook list) : Column list =
            let createColumn (identifier: string) =
                let field = Fields.allFields |> List.find (fun f -> f.Table = identifier)
                let selector = field.Replacer 
                let values = books |> List.map (selector >> Option.getOrElse String.Empty >> (fun s -> (s, s.Length)))
                let header = field.Name 
                let maxWidth = Math.Min(Math.Max(values |> List.maxBy snd |> snd, header.Length), maxColumnWidth)
                {
                    Width = maxWidth
                    Header = header
                    Cells = values |> List.map (fun (content, _) -> { Content = content; Width = maxWidth })
                }
            identifiers |> List.map createColumn
            
        let apply (maxColumnWidth: int) (tableFormat: string) (books: Audiobook.Audiobook list) =
            let columnIdentifiers = elementRegex.Matches(tableFormat) |> Seq.map (fun m -> m.Value) |> List.ofSeq
            let validIdentifiers = columnIdentifiers |> Utilities.List.splitBy (fun identifier -> Fields.allFields |> List.exists (fun f -> f.Table = identifier))
            do if validIdentifiers.NonMatching.IsEmpty then ()
               else printfn "The following format identifiers are unknown and will be skipped: %s" (String.Join(", ", validIdentifiers.NonMatching))
            
            if validIdentifiers.Matching.IsEmpty then []
            else
                let columns = books |> createColumns maxColumnWidth validIdentifiers.Matching
                let rows = columns |> columnsToRows
                (columns |> createHeader) @ (rows |> List.map createRow) @ (columns |> createFooter)
            
    module Html =
        open System
        open RazorLight
        open b0wter.FSharp.Operators
        
        type BookViewmodel = {
            Artist: string
            Album: string
            AlbumArtist: string
            Completed: bool
            Aborted: bool
            Comment: string
            Rating: int
            Title: string
            Duration: TimeSpan
            Id: int
            Genre: string
        }
        
        type Viewmodel = {
            Books: BookViewmodel list
            Generated: DateTime
        }
        
        let toViewModel (book: Audiobook.Audiobook) =
            {
                Artist = book.Artist |?| String.Empty
                Album = book.Album |?| String.Empty
                AlbumArtist = book.AlbumArtist |?| String.Empty
                Completed = book.State = Audiobook.State.Completed
                Aborted = book.State = Audiobook.State.Aborted
                Comment = book.Comment |?| String.Empty
                Rating = book.Rating |> Option.map Rating.value |?| 0
                Title = book.Title |?| String.Empty
                Duration = book.Duration
                Id = book.Id
                Genre = book.Genre |?| String.Empty
            }
  
        let engine<'a> () =
            RazorLightEngineBuilder()
                .UseEmbeddedResourcesProject(typeof<'a>)
                .SetOperatingAssembly(typeof<'a>.Assembly)
                .UseMemoryCachingProvider()
                .Build()
 
        let apply (template: string) (books: Audiobook.Audiobook list) =
            let viewmodel = { Books = books |> List.map toViewModel; Generated = DateTime.Now }
            let result = (engine<Viewmodel>().CompileRenderStringAsync("templatekey", template, viewmodel)).GetAwaiter().GetResult()
            result