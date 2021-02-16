namespace b0wter.Fbrary

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
                    {| NonMatching = nonMatchingAccumulator; Matching = matchingAccumulator |}
                | head :: tail when head |> predicate ->
                    step nonMatchingAccumulator (head :: matchingAccumulator) tail
                | head :: tail ->
                    step (head :: nonMatchingAccumulator) matchingAccumulator tail
            step [] [] items
                