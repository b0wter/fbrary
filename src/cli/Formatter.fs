namespace b0wter.Fbrary

open System.Text.RegularExpressions
open b0wter.Fbrary.Audiobook

module Formatter =
    type FormatReplacer = Audiobook.Audiobook -> string option
    type FormatReplacerWithFieldName = (string * FormatReplacer)
    
    module CommandLine =
        
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
            
        
