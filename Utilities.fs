namespace b0wter.Audiobook

module Utilities =
    
    module Result =
        
        let fromOption (errorCase: 'a) (o: 'b option) : Result<'b, 'a> =
            match o with
            | Some b -> Ok b
            | None -> Error (errorCase)

    module String =
        
        let contains (searchString: string) (content: string) =
            content.Contains(searchString)