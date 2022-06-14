module upgradeActor

open System.Collections.Generic

type CoordinatorMessage =
    | Job of int
    | Ready
    | RequestJob of AsyncReplyChannel<int>

type SortDataMessage =
    | Agent1 of int
    | Agent2 of int
    | Agent3 of int
    | Agent4 of int

let vals1 = new ResizeArray<int * int> ()
let vals2 = new ResizeArray<int * int> ()
let vals3 = new ResizeArray<int * int> ()
let vals4 = new ResizeArray<int * int> ()

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

let SortData =
    MailboxProcessor<SortDataMessage>.Start (fun inbox ->
        let rec loop () =
            async {
                let! message = inbox.Receive ()
                match message with
                | Agent1 w -> (vals1.Count, w) |> vals1.Add
                | Agent2 w -> (vals2.Count, w) |> vals2.Add
                | Agent3 w -> (vals3.Count, w) |> vals3.Add
                | Agent4 w -> (vals4.Count, w) |> vals4.Add
                return! loop ()}
        loop ())

let Worker agentNo =
    MailboxProcessor<bool>.Start (fun inbox ->
        Coordinator.Post Ready
        let rec loop () =
            async { 
                let! length = Coordinator.PostAndAsyncReply (fun reply -> RequestJob reply)
                let byAgent = 
                    match agentNo with
                    | 1 -> Agent1 length
                    | 2 -> Agent2 length
                    | 3 -> Agent3 length
                    | 4 -> Agent4 length
                    | _ -> failwith "Unknown agent"

                SortData.Post byAgent
                do! Async.Sleep length
                return! loop ()}
        loop ())


// Create fake jobs
let rand = new System.Random(941345563)
Seq.init 500 (fun _ -> rand.Next(1, 101))
|> Seq.iter (fun r -> Coordinator.Post(Job r))


// Create worker agents
let JobAgent1 = Worker 1
let JobAgent2 = Worker 2
let JobAgent3 = Worker 3
let JobAgent4 = Worker 4