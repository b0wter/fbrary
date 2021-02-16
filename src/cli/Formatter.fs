namespace b0wter.Fbrary

open System.Text.RegularExpressions
open b0wter.Fbrary.Audiobook

module Formatter =
    module RatingSymbols =
        let empty = '░'
        let filled = '█'
        
        let emptyCircle = '○'
        let filledCircle = '●'
        let dottedCircle = '◌'
            
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
    
    let private ratingString = "rating_string" |> asReplace
    let ratingStringFormatString = ratingString, fun (a: Audiobook.Audiobook) ->
         match a.Rating |> Option.map Rating.value with
         | Some i -> Some <| sprintf "%i/%i" i Rating.maxValue
         | None -> Some <| sprintf "-/%i" Rating.maxValue
                                                                     
    let private ratingDots = "rating_dots" |> asReplace
    let ratingDotsFormatString = ratingDots, fun (a: Audiobook.Audiobook) ->
         match a.Rating |> Option.map Rating.value with
         | Some i -> Some <| System.String(RatingSymbols.filledCircle, i) + System.String(RatingSymbols.emptyCircle, Rating.maxValue - i)
         | None -> Some <| System.String(RatingSymbols.dottedCircle, Rating.maxValue)
    
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
        ratingStringFormatString
        ratingDotsFormatString
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
            let hLine = '━'
            let vLine = '┃'
            let cUpperLeft = '┏'
            let cUpperRight = '┓'
            let cLowerLeft = '┗'
            let cLowerRight = '┛'
            let center = '╋'
            let tUpper = '┳'
            let tLower = '┻'
            let tLeft = '┣'
            let tRight = '┫'
            
            
        module TwinBorder =
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
            let doubleVTLeft = '┠'
            let doubleVTRight = '┨'
            //let doubleVTLowerLeft = '╚'
            //let doubleVTLowerRight = '╝'
            let doubleHTUpper = '┯'
            let doubleHTLower = '┷'
            let cross = '╇'
            
        module SimpleTwinConnector =
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
            (ratingString, "Rating")
            (ratingDots, "Rating")
            (duration, "Duration")
            (id, "Id")
        ]
        
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
                line DoubleBorder.cUpperLeft SimpleDoubleConnector.doubleHTUpper DoubleBorder.cUpperRight DoubleBorder.hLine DoubleBorder.hLine boxContent
                line DoubleBorder.vLine SimpleBorder.vLine DoubleBorder.vLine ' ' ' ' textContent
                line SimpleDoubleConnector.doubleVTLeft SimpleBorder.center DoubleBorder.tRight SimpleBorder.hLine SimpleBorder.hLine boxContent
            ]
                
        let createRow (row: Row)=
            //let boxContent = row.Cells |> List.map (fun r -> ("", r.Width))
            let cutText (maxLength: int) (s: string) : string =
                if s.Length > maxLength then
                    s.Substring(0 ,maxLength - 3) + "..."
                else
                    s
            let textContent = row.Cells |> List.map (fun r -> (r.Content |> (cutText r.Width), r.Width))
            line DoubleBorder.vLine SimpleBorder.vLine DoubleBorder.vLine ' ' ' ' textContent
            
        let createFooter (columns: Column list) =
            let boxContent = columns |> List.map (fun c -> ("", c.Width))
            line DoubleBorder.cLowerLeft SimpleDoubleConnector.doubleHTLower DoubleBorder.cLowerRight DoubleBorder.hLine DoubleBorder.hLine boxContent
                
        let createColumns maxColumnWidth (identifiers: string list) (books: Audiobook.Audiobook list) : Column list =
            let createColumn (identifier: string) =
                let selector = allFormatStrings |> List.find (fun (f, _) -> f = identifier) |> snd
                let values = books |> List.map (selector >> Option.getOrElse String.Empty >> (fun s -> (s, s.Length)))
                let header = tableHeaders |> List.find (fun (key, _) -> key = identifier) |> snd
                let maxWidth = Math.Min(Math.Max(values |> List.maxBy snd |> snd, header.Length), maxColumnWidth)
                {
                    Width = maxWidth //Math.Min(Math.Max(maxWidth, identifier.Length), maxColumnWidth)
                    Header = header // tableHeaders |> List.find (fun (key, _) -> key = identifier) |> snd
                    Cells = values |> List.map (fun (content, _) -> { Content = content; Width = maxWidth })
                }
            identifiers |> List.map createColumn
            
        let apply (maxColumnWidth: int) (tableFormat: string) (books: Audiobook.Audiobook list) =
            let books = books |> List.sortBy (fun b -> b.Id)
            let columnIdentifiers = elementRegex.Matches(tableFormat) |> Seq.map (fun m -> m.Value) |> List.ofSeq
            let validIdentifiers = columnIdentifiers |> Utilities.List.splitBy (fun identifier -> allFormantPlaceholders |> List.contains identifier)
            do if validIdentifiers.NonMatching.IsEmpty then ()
               else printfn "The following format identifiers are unknown and will be skipped: %s" (String.Join(", ", validIdentifiers.NonMatching))
            let columns = books |> createColumns maxColumnWidth validIdentifiers.Matching
            let rows = columns |> columnsToRows
            (columns |> createHeader) @ (rows |> List.map createRow) @ [ columns |> createFooter ]
            
            