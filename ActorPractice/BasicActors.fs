module BasicActor

type Message = string * AsyncReplyChannel<string>

let firstAgent =
    MailboxProcessor.Start (fun inbox ->
        async {
            while true do
                let! msg = inbox.Receive()
                printfn "got message '%s'" msg
        })

let replyAgent =
    MailboxProcessor<Message>.Start (fun inbox ->
        let rec loop () =
            async { 
                let! (message, replyChannel) = inbox.Receive()
                replyChannel.Reply($"Received message: {message}")
                do! loop ()
            }
        loop ())

let scanAgent =
    MailboxProcessor.Start (fun inbox ->
        async {
            while true do
                do! inbox.Scan (fun hello ->
                    match hello with
                    | "Hello!" -> 
                        Some(async { printfn "This is a hello message!"})
                    | _ -> None)
                let! msg = inbox.Receive()
                printfn "Got message '%s'" msg})


["1"; "2"; "3"; "4"; "5"; "6"; "7"; "8"; "9"; "10";
"Hello!"; "Hello!"; "Hello!"; "Hello!"; "Hello!"; "Hello!"; 
"Hello!"; "Hello!"; "Hello!"; "Hello!"]
|> List.map scanAgent.Post |> ignore

firstAgent.Post "Hello!"

let receive = replyAgent.PostAndReply (fun rc -> "Hello", rc) // "Received message: Hello"

printfn "%s" receive