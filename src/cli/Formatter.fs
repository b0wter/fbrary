namespace b0wter.Fbrary

open System.Text.RegularExpressions
open b0wter.Fbrary.Audiobook

module Formatter =
    type FormatReplacer = Audiobook.Audiobook -> string option
    type FormatReplacerWithFieldName = (string * FormatReplacer)
    
    // If you change the regex you probably need to change `optionalSymbol` as well!
    let optionalElementsRegex = Regex("(\?\?)[^\?]*(\?\?)") 
    let optionalSymbol = "??" // If you change the symbol you need to change the regex as well!
    let optionalSymbolLength = optionalSymbol.Length
    
    // If you change the regex you probably need to change `replaceSymbol` as well!
    let elementRegex = Regex("(%)[^\%]*(%)") 
    let replaceSymbol = "%" // If you change the symbol you need to change the regex as well!
    let replaceSymbolLength = replaceSymbol.Length
    
    let durationFormat = "h\:mm"
    
    let asReplace (s: string) = sprintf "%s%s%s" replaceSymbol s replaceSymbol
    
    let private artist = "artist" |> asReplace
    let artistFormatString = artist, fun (a: Audiobook.Audiobook) -> a.Artist
    
    let private album = "album" |> asReplace
    let albumFormatString = album, fun (a: Audiobook.Audiobook) -> a.Album
    
    let private title = "title" |> asReplace
    let titleFormatString = title, fun (a: Audiobook.Audiobook) -> a.Title
    
    let private albumArtist = "albumartist" |> asReplace
    let albumArtistFormatString = albumArtist, fun (a: Audiobook.Audiobook) -> a.AlbumArtist
    
    let private duration = "duration" |> asReplace
    let durationFormatString = duration, fun (a: Audiobook.Audiobook) -> a.Duration.ToString(durationFormat) |> Some
    
    let private id = "id" |> asReplace
    let idFormatString = id, fun (a: Audiobook.Audiobook) -> a.Id |> string |> Some
    
    let private rating = "rating" |> asReplace
    let ratingFormatString = rating, fun (a: Audiobook.Audiobook) -> match a.Rating |> Option.map Rating.value with
                                                                     | Some i -> Some <| sprintf "%i/%i" i Rating.maxValue
                                                                     | None -> Some <| sprintf "-/%i" Rating.maxValue
    
    let private comment = "comment" |> asReplace
    let commentFormatString = comment, fun (a: Audiobook.Audiobook) -> a.Comment
    
    let private genre = "genre" |> asReplace
    let genreFormatString = genre, fun (a: Audiobook.Audiobook) -> a.Genre
    
    let (allFormatStrings : FormatReplacerWithFieldName list) = [
        artistFormatString
        albumFormatString
        titleFormatString
        albumArtistFormatString
        durationFormatString
        idFormatString
        ratingFormatString
        commentFormatString
        genreFormatString
    ]
    let allFormantPlaceholders = allFormatStrings |> List.map fst
    
    module CommandLine =
        
        let defaultFormatString = "(%id%) %album% ??(%artist%) ??%duration%"
        
        /// Tries to replace a single format identifier (%%..%%) with a proper value.
        /// `target` is a simple format identifier like %%artist%%
        let applyToSingleFormatString (ifNotFound: string -> Audiobook.Audiobook -> string) (breakIfNotFound: bool) (target: string) (a: Audiobook.Audiobook) =
            let rec step (accumulator: string) (remainingReplacers: FormatReplacerWithFieldName list) =
                match remainingReplacers with
                | [] -> Some accumulator
                | (expression, replacer) :: tail ->
                    if expression = target then
                        match a |> replacer with
                        | Some text ->
                            let newAccumulator = accumulator.Replace(expression, text)
                            step newAccumulator tail
                        | None when breakIfNotFound ->
                            None
                        | None ->
                            let newAccumulator = ifNotFound expression a
                            step newAccumulator tail
                    else
                        step accumulator tail
            step target allFormatStrings
            
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
        
        module SimpleBorder = 
            let hLine = '─'
            let vLine = '│'
            let cUpperLeft = '┌'
            let cLowerLeft = '└'
            let cLowerRight = '┘'
            let cUpperRight = '┐'
            let center = '┼'
            let tUpper = '┬'
            let tLower = '┴'
            let tLeft = '├'
            let rRight = '┤'
            
        module DoubleBorder =
            let hLine = '═'
            let vLine = '║'
            let cUpperLeft = '╔'
            let cUpperRight = '╗'
            let cLowerLeft = '╚'
            let cLowerRight = '╝'
            let center = '╬'
            let tUpper = '╦'
            let tLower = '╩'
            let tLeft = '╠'
            let tRight = '╣'
            
        module SimpleDoubleConnector =
            let doubleVTLeft = '╟'
            let doubleVTRight = '╢'
            let doubleVTLowerLeft = '╚'
            let doubleVTLowerRight = '╝'
            let doubleHTLower = '╧'
            let cross = '╬'
            
        let tableHeaders = [
            (artist, "Artist")
            (album, "Album")
            (albumArtist, "Album Artist")
            (title, "Title")
            (comment, "Comment")
            (genre, "Genre")
            (rating, "Rating")
            (duration, "Duration")
            (id, "Id")
        ]
        
        type Cell = {
            Content: string
            Length: int
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
                
                
        let separator leftBorder line columnBorder rightBorder (widths: int list) =
            let rec fold (accumulator: string) (remaining: int list) (isFirst: bool) =
                match remaining with
                | [] ->
                    accumulator
                | [ single ] when isFirst ->
                    sprintf "%c%s%c" leftBorder (String(line, single + 2)) rightBorder
                | [ last ] ->
                    let newPart = (sprintf "%s%c" (String(line, last + 2)) rightBorder)
                    accumulator + newPart
                | head :: tail when isFirst ->
                    let newPart = sprintf "%c%s%c" leftBorder (String(line, head + 2)) columnBorder
                    fold (accumulator + newPart) tail false
                | head :: tail ->
                    let newPart = sprintf "%s%c" (String(line, head + 2)) columnBorder
                    fold (accumulator + newPart) tail false
            fold String.Empty widths true
            
        let createHeader (columns: Column list) =
            match columns with
            | [ single ] ->
                // The header consists of a single column.
                [
                    sprintf "%c%s%c"   DoubleBorder.cUpperLeft (String(DoubleBorder.hLine, single.Width + 2)) DoubleBorder.cUpperRight
                    sprintf "%c %s %c" DoubleBorder.vLine (single.Header.PadRight(single.Width)) DoubleBorder.vLine
                    sprintf "%c%s%c"   SimpleDoubleConnector.doubleVTLeft (String(DoubleBorder.hLine, single.Width + 2)) SimpleDoubleConnector.doubleVTRight
                ]
            | [ first; second ] ->
                [
                    sprintf "%c%s%c%s%c"     DoubleBorder.cUpperLeft (String(DoubleBorder.hLine, first.Width + 2)) DoubleBorder.tUpper (String(DoubleBorder.hLine, second.Width + 2)) DoubleBorder.cUpperRight
                    sprintf "%c %s %c %s %c" DoubleBorder.vLine (first.Header.PadRight(first.Width)) DoubleBorder.vLine (second.Header.PadRight(second.Width)) DoubleBorder.vLine
                    sprintf "%c%s%c%s%c"     SimpleDoubleConnector.doubleVTLeft (String(DoubleBorder.hLine, first.Width + 2)) SimpleDoubleConnector.cross (String(DoubleBorder.hLine, second.Width + 2)) SimpleDoubleConnector.doubleVTRight
                ]
            | many ->
                [ separator SimpleDoubleConnector.doubleVTLeft SimpleBorder.hLine SimpleBorder.center SimpleDoubleConnector.doubleVTRight (many |> List.map (fun c -> c.Width)) ]
                //let widths = many |> List.map (fun c -> c.Width)
                //[  ]
                (*
                let first = many |> List.head
                let last = many |> List.last
                let middle = many |> List.take (many.Length - 2) |> List.skip 1
                [ contentSeparator ]
                *)
                
        let createColumns (identifiers: string list) (books: Audiobook.Audiobook list) : Column list =
            let createColumn (identifier: string) =
                let selector = allFormatStrings |> List.find (fun (f, _) -> f = identifier) |> snd
                let values = books |> List.map (selector >> Option.getOrElse String.Empty >> (fun s -> (s, s.Length)))
                let maxWidth = values |> List.maxBy snd |> snd
                {
                    Width = Math.Max(maxWidth, identifier.Length)
                    Header = tableHeaders |> List.find (fun (key, value) -> key = identifier) |> snd
                    Cells = values |> List.map (fun (content, length) -> { Content = content; Length = length })
                }
            identifiers |> List.map createColumn
            
        let apply (tableFormat: string) (books: Audiobook.Audiobook list) =
            let columnIdentifiers = elementRegex.Matches(tableFormat) |> Seq.map (fun m -> m.Value) |> List.ofSeq
            let validIdentifiers = columnIdentifiers |> Utilities.List.splitBy (fun identifier -> allFormantPlaceholders |> List.contains identifier)
            do if validIdentifiers.NonMatching.IsEmpty then ()
               else printfn "The following format identifiers are unknown and will be skipped: %s" (String.Join(", ", validIdentifiers.NonMatching))
            let columns = books |> createColumns columnIdentifiers
            columns |> createHeader
            
            