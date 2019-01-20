
module Helpers

    open System.Collections.Generic
    open System.Threading
    open System.Threading.Tasks
    
    let memoizations = Dictionary<obj, CancellationTokenSource>(HashIdentity.Structural)

    let debounce<'T> =
        fun (timeout: int) (doNothingMsg:'T) (msg:'T)  ->
            
            let key = msg.GetType()
            match memoizations.TryGetValue(key) with
            | true, previousCts -> previousCts.Cancel()
            | _ -> ()

            let cts = new CancellationTokenSource()
            let tsc = new TaskCompletionSource<'T>()
            memoizations.[key] <- cts

            new Timer(
                (fun _ -> 
                    match cts.IsCancellationRequested with
                    | true -> 
                        tsc.SetResult(doNothingMsg)
                        ()
                    | false ->
                        memoizations.Remove(key) |> ignore
                        //fn value
                        tsc.SetResult(msg)
                ),null,timeout,Timeout.Infinite) |> ignore
            tsc.Task |> Async.AwaitTask
           


    let asyncFunc f = 
        async {  
            let original = System.Threading.SynchronizationContext.Current
            do! Async.SwitchToNewThread() 
            let result = f() 
            do! Async.SwitchToContext(original)
            return result 
        }

    open Elmish.WPF.Binding

   
            

