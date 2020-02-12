namespace FS
open System.Collections.Generic
open Godot
open Godot.Collections
open Newtonsoft.Json
open Newtonsoft.Json
open Newtonsoft.Json
open WebSocketSharp
open WebSocketSharp.Server
module Exts =
    type Array with
        member this.ToList<'a>() =
            seq {
                for c in this do
                    yield (c :?> 'a)
            } |> List.ofSeq
        
open Exts
open Newtonsoft.Json
open Newtonsoft.Json

type Message =
    | JoinRequest
    | JoinedResponse of int 
    | SetMousePosition of Vector2
    | SetPositions of Vector2 list


        
        
        
and ServerFs() =
    inherit Node()
    static member val ws = lazy (
        let ret = new WebSocket("ws://127.0.0.1:8080/lobby")
        ret.ConnectAsync()
        
        GD.Print "Starting WSSSSS"
        ret
    )


and PlayerFs() as this =
    inherit KinematicBody2D()
    member val mouse_pos = Vector2.Zero with get, set
    
    override this._Ready() =
        GD.Print "Hi from f#"
        
    
    override this._Process(delta) =
//        this.MoveAndSlide((this.mouse_pos - this.GlobalPosition).Normalized()*200.0f)
        
        ()
and MainFs() as this =
    inherit Node2D()
    override this._Ready() =
        
        ServerFs.ws.Value.OnOpen.Add(
                                        fun x ->
                                            GD.Print "Success"
                                            ServerFs.ws.Value.SendAsync(JsonConvert.SerializeObject(JoinRequest), null)
                                    )
        ServerFs.ws.Value.OnMessage.Add(
                                           fun m ->
                                               match (JsonConvert.DeserializeObject<Message> (m.Data)) with
                                               | SetPositions xs ->
//                                                   GD.Print "Receiving positions from server"
//                                                   GD.Print xs 
                                                   
                                                   for (c, p) in Seq.zip xs (this.GetChildren().ToList<PlayerFs>()) do
                                                       p.GlobalPosition <- c
                                                | _ -> ()
                                       )
    override this._Process(delta) =
        ServerFs.ws.Value.SendAsync(JsonConvert.SerializeObject(SetMousePosition(this.GetGlobalMousePosition())), null) 
