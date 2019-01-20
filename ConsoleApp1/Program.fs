namespace ConsoleApp1

    open System
    open Elmish
    open Elmish.WPF
    open FSharp.Control.Reactive
    open FsXaml
    open Helpers



    type Contact = 
        { Name:string
          }

    type Facade () =
        member this.GetContacts () =
            seq {
                for i in 1..1000 do
                    yield { Name = sprintf "name %i" i }
            }


    type MainWindow = XAML<"MainWindow.xaml">


       
    type Model =
        {   Contacts: Contact seq
            ContactsFilter: string }

    type Msg =
        | SetContactsFilter of string
        | ContactFilterSet of string
        | UpdateContacts of Contact seq
        | DoNothing
        
    
    /// Wraps the Rolodex MVU bits in a class with an injectable facade.
    type RolodexMVU(facade: Facade) =
        let contacts = facade.GetContacts()

        let init () =
            {   Contacts = []
                ContactsFilter = ""
                 }
            , Cmd.none


        let update msg m =
            match msg with
            | SetContactsFilter filterText -> 
                let cmd () = (ContactFilterSet filterText) |> (debounce 500) DoNothing
                m, Cmd.ofAsync cmd () id (fun exn -> DoNothing)
            | ContactFilterSet filterText -> 
                let filterCommand () =
                    async {
                        let! c = asyncFunc (fun () ->
                            System.Threading.Thread.Sleep 5000
                            contacts |> Seq.filter (fun c -> c.Name.ToLower().Contains(filterText.ToLower()))
                        )
                        
                        return UpdateContacts c
                    } 

                { m with ContactsFilter = filterText }, Cmd.ofAsync filterCommand () id (fun exn -> DoNothing)
            | UpdateContacts contacts ->
                { m with Contacts = contacts }, Cmd.none
            | DoNothing ->
                m, Cmd.none
            
                
        let bindings model dispatch =
            [
                "Contacts" |> Binding.oneWay (fun m -> m.Contacts)
                "ContactsFilter" |> Binding.twoWay
                    (fun m -> m.ContactsFilter)
                    (fun v m -> 
                        v |> SetContactsFilter                        
                    )
            ]
 
        //let rxSubcription model =
        //    Cmd.ofSub (fun dispatch -> 
        
        //        // Subscribe to observable
        //        model.ContactsFilterChanged
        //        |> Observable.throttle (TimeSpan.FromMilliseconds(500.0))
        //        |> Observable.distinctUntilChanged
        //        |> Observable.subscribe (fun filterText ->
        //            filterText |> SetContactsFilter |> dispatch
        //        ) 
        //        |> ignore

        //        model.ContactsFilterChanged 
        //        |> Subject.onNext "" // "Prime the Pump"
        //        |> ignore
        //    )

        member this.ShowView() =
            Program.mkProgram init update bindings
            //|> Program.withSubscription rxSubcription
            |> Program.withConsoleTrace
            |> Program.runWindowWithConfig
                { ElmConfig.Default with LogConsole = true }
                (MainWindow())


    module Main = 

        [<EntryPoint; STAThread>]
        let main argv =
            let mvu = new RolodexMVU(new Facade())
            mvu.ShowView()