module ExampleActor

open System.Collections.Generic

type CoordinatorMessage =
    | Job of int
    | Ready
    | RequestJob of AsyncReplyChannel<int>

let Coordinator =
    MailboxProcessor<CoordinatorMessage>.Start (fun inbox ->
        let queue = new Queue<int> ()
        let mutable count = 0
        let rec loop () =
            async {
                while count < 4 do
                    do! inbox.Scan (function
                        | Ready -> Some (async { count <- count + 1 })
                        | Job _ -> None
                        | RequestJob _ -> None)
                let! message = inbox.Receive ()
                match message with
                | Job length -> queue.Enqueue length
                | RequestJob replyChannel -> replyChannel.Reply <| queue.Dequeue ()
                | Ready -> ()                
                return! loop ()}
        loop ())

let Worker () =
    MailboxProcessor<bool>.Start (fun inbox ->
        Coordinator.Post Ready
        let rec loop () =
            async { 
                let! length = Coordinator.PostAndAsyncReply RequestJob
                do! Async.Sleep length
                return! loop ()}
        loop ())


// Create fake jobs
let rand = new System.Random(941345563)
Seq.init 500 (fun _ -> rand.Next(1, 101))
|> Seq.iter (fun r -> Coordinator.Post(Job r))


// Create worker agents
let JobAgent1 = Worker ()
let JobAgent2 = Worker ()
let JobAgent3 = Worker ()
let JobAgent4 = Worker ()