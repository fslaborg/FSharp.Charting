
namespace global

module Event = 
    /// An event which triggers on every 'n' triggers of the input event
    let every n (ev:IEvent<_>) = 
        let out = new Event<_>()
        let count = ref 0 
        ev.Add (fun arg -> incr count; if !count % n = 0 then out.Trigger arg)
        out.Publish

    /// An event which triggers on every event once at least 'n' samples are available, reporting the last 'n' samples
    let window n (ev:IEvent<_>) = 
        let out = new Event<_>()
        let queue = System.Collections.Generic.Queue<_>()
        ev.Add (fun arg -> queue.Enqueue arg; 
                           if queue.Count >= n then 
                                out.Trigger (queue.ToArray()); 
                                queue.Dequeue() |> ignore)
        out.Publish

    /// An event which triggers on every event, reporting at most the last 'n' samples
    let windowAtMost n (ev:IEvent<_>) = 
        let out = new Event<_>()
        let queue = System.Collections.Generic.Queue<_>()
        ev.Add (fun arg -> queue.Enqueue arg; 
                           out.Trigger (queue.ToArray()); 
                           if queue.Count >= n then 
                                queue.Dequeue() |> ignore)
        out.Publish

    /// An event which triggers on every event, reporting samples from the given time window
    let windowTimeInterval (interval:int) (ev:IEvent<System.DateTime * _>) = 
        let out = new Event<_>()
        let queue = System.Collections.Generic.Queue<_>()
        ev.Add (fun arg -> queue.Enqueue arg; 
                           while (System.DateTime.Now - fst (queue.Peek())).TotalMilliseconds > float interval  do
                                queue.Dequeue() |> ignore
                           out.Trigger (queue.ToArray()))
        out.Publish

    /// An event which triggers at regular intervals reporting the latest observed value of the given event
    let sampled interval (ev:IEvent<_>) = 
        let out = new Event<_>()
        let latest = ref None
        let timer = new System.Windows.Forms.Timer(Interval=interval, Enabled=true)
        timer.Tick.Add (fun args -> match latest.Value with None -> () | Some x -> out.Trigger (System.DateTime.Now,x))
        timer.Start()
        ev.Add (fun arg -> latest := Some arg)
        out.Publish

    /// An event which triggers at regular intervals reporting the real world time at each trigger
    let clock interval = 
        let out = new Event<_>()
        let timer = new System.Windows.Forms.Timer(Interval=interval, Enabled=true)
        timer.Tick.Add (fun args -> out.Trigger System.DateTime.Now)
        timer.Start()
        out.Publish

    let pairwise  (ev:IEvent<_>) = 
        let out = new Event<_>()
        let queue = System.Collections.Generic.Queue<_>()
        ev.Add (fun arg -> queue.Enqueue arg; 
                           if queue.Count >= 2 then 
                                let elems = queue.ToArray()
                                out.Trigger (elems.[0], elems.[1])
                                queue.Dequeue() |> ignore)
        out.Publish

