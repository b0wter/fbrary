namespace b0wter.AudiobookLibrary

module Utilities =
    
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
                