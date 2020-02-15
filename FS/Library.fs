namespace FS
open Godot
open Godot.Collections
open Newtonsoft.Json
module Exts =
    type Array with
        member this.ToList<'a>() =
            seq {
                for c in this do
                    if c :? 'a then  
                        yield (c :?> 'a)
            } |> List.ofSeq
    
    let add' f e =
        Event.add f e
        e        
        
open System.Text
open Exts
type Message =
    | JoinRequest
    | JoinedResponse of int 
    | SetMousePosition of Vector2
    | SetPositions of Vector2 list

type WebSocketClient'(url : string) as this=
    inherit WebSocketClient()
    
    do
        this.ConnectToUrl url |> ignore
        this.Connect("connection_established", this, "on_connected") |> ignore
        this.Connect("data_received", this, "on_message") |> ignore
    let mutable connected = false    
    let _OnConnected = new Event<_>()
    let _OnMessage = new Event<Message>()
    member val OnConnected = _OnConnected.Publish
    member val OnMessage = _OnMessage.Publish
    member this.on_connected(protocol: obj[]) =
        connected <- true
        _OnConnected.Trigger()
    member this.on_message() = _OnMessage.Trigger(JsonConvert.DeserializeObject<Message>(Encoding.ASCII.GetString(this.GetPeer(1).GetPacket())))
    member this.send (msg:Message) = if connected then do this.GetPeer(1).PutPacket(JsonConvert.SerializeObject(msg).ToAscii()) |> ignore
        
and ServerFs() =
    inherit Node()
    static member val ws = lazy (
        let ret = new WebSocketClient'("ws://192.168.0.14:8080/lobby")
        ret
    )


and PlayerFs() as this =
    inherit KinematicBody2D()
    member val mouse_pos = Vector2.Zero with get, set
    override this._Process(delta) =
        ()
    
and MainFs() as this =
    inherit Node2D()
    override this._Ready() =
        
        ServerFs.ws.Value.OnConnected
        |> Exts.add' (fun (x) -> GD.Print "Connection Established" )
        |> Exts.add' (fun (x) -> GD.Print "Hello again")
        |> ignore
        
        ServerFs.ws.Value.OnMessage
        |> Exts.add' (
                        fun m ->
                            match m with
                            | SetPositions xs ->
                                for (c, p) in Seq.zip xs (this.GetChildren().ToList<PlayerFs>()) do
                                    p.GlobalPosition <- c
                            | _ -> ()
                    )
        |> ignore
        
    override this._Process(delta) =
        ServerFs.ws.Value.Poll()
        ServerFs.ws.Value.send <| SetMousePosition(this.GetGlobalMousePosition())
