namespace b0wter.Fbrary.Core

module Utilities =
    module Parsers =
        let int (s: string) : Result<int, string> =
            let success, value = s |> System.Int32.TryParse
            if success then Ok value
            else Error "The given string cannot be converted to a number."
    
    module Result =
        let fromOption (errorCase: 'a) (o: 'b option) : Result<'b, 'a> =
            match o with
            | Some b -> Ok b
            | None -> Error (errorCase)
            
    module String =
        let contains (searchString: string) (content: string) =
            content.Contains(searchString)
            
    module List =
        let splitBy (predicate: 'a -> bool) (items: 'a list) =
            let rec step (nonMatchingAccumulator: 'a list) (matchingAccumulator: 'a list) (remaining: 'a list) =
                match remaining with
                | [] ->
                    {| NonMatching = nonMatchingAccumulator |> List.rev; Matching = matchingAccumulator |> List.rev |}
                | head :: tail when head |> predicate ->
                    step nonMatchingAccumulator (head :: matchingAccumulator) tail
                | head :: tail ->
                    step (head :: nonMatchingAccumulator) matchingAccumulator tail
            step [] [] items
            
        let foldResult (f: 'State -> 'T -> Result<'State, 'Error>) (initialValue: 'State) (items: 'T list) =
            let rec step (state: 'State) (remaining: 'T list) : Result<'State, 'Error> =
                match remaining with
                | [] -> Ok state
                | head :: tail ->
                    match f state head with
                    | Ok o ->
                        step o tail
                    | Error e -> Error e
            step initialValue items

        let skipEmpty (lists: 'a list list) =
            lists |> List.filter (not << List.isEmpty)