namespace b0wter.Fbrary

open System.Text.RegularExpressions

module Formatter =
    type FormatReplacer = Audiobook.Audiobook -> string option
    type FormatReplacerWithFieldName = (string * FormatReplacer)
    
    type CommandLine() =
        let optionalElementsRegex = Regex("(\?\?)[^\?]*(\?\?)")
        let elementRegex = Regex("(%%)[^\%]*(%%)")
        
        let alwaysReplaceSymbol = "%%"
        let replaceIfSomeSymbol = "??"
        
        let artistFormatString = "%%artist%%", fun (a: Audiobook.Audiobook) -> a.Artist
        let albumFormatString = "%%album%%"
        let titleFormatString = "%%title%%"
        let albumArtistFormatString = "%%albumartist%%"
        let durationFormatString = "%%duration%%"
        let idFormatString = "%%id%%"
        let ratingFormatString = "%%rating%%"
        let commentFormatString = "%%comment%%"
        
        let (allFormatStrings : FormatReplacerWithFieldName list) = [ artistFormatString ]
        
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
            
        let applySingleOptional (optionalMatch: string) (a: Audiobook.Audiobook) =
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
            
        let applyAllOptional (format: string) (a: Audiobook.Audiobook) =
            let elements = optionalElementsRegex.Matches(format) |> Seq.map (fun m -> m.Value)
            Seq.fold (fun (state: string) (next: string) ->
                    match applySingleOptional next a with
                    | Some text -> state.Replace(next, text)
                    | None -> System.String.Empty
                ) format elements
        
        let applyTo (format: string) (a: Audiobook.Audiobook) =
            let apply (ifNone: string -> string) ((key: string), (retriever: Audiobook.Audiobook -> string option)) : string =
                match a |> retriever with
                | Some value -> value
                | None -> ifNone key
            let applyAlwaysFormatter = apply (sprintf "<no %s set>")
            let applyIfSomeFormatter = apply (fun _ -> System.String.Empty)
            
            
            //let alwaysFormatter = formatters |> List.map (fun symbol book -> alwaysReplaceSymbol + symbol + alwaysReplaceSymbol, book)
            
            0
        
        (*
        let allFormatStrings = [ artistFormatString; albumArtistFormatString
                                 albumFormatString; titleFormatString; durationFormatString
                                 idFormatString; ratingFormatString; commentFormatString ]
        
        let alwaysFormatStrings = allFormatStrings |> List.map (fun s -> sprintf "%s%s%s" alwaysReplaceSymbol s alwaysReplaceSymbol)
        let ifSomeFormatStrings = allFormatStrings |> List.map (fun s -> sprintf "%s%s%s" replaceIfSomeSymbol s replaceIfSomeSymbol)
        
        let applyTo (format: string) (a: Audiobook.Audiobook) =
            let formatters = 
            List.fold (|>) format replacers
            *)
