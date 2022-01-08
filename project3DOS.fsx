#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
open System
open Akka.Actor
open Akka.TestKit
open Akka.FSharp
open Akka.Configuration
open System.Collections.Generic
open System.Security.Cryptography
open System.Text
open System.Numerics

//Creating the system ------->
let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            loglevel : ERROR
            # Options: OFF, ERROR, WARNING, INFO, DEBUG
            loglevel = ""OFF""
        }")
// Creating an ActorSytem for creation and usage of actors
let WholeSystem = ActorSystem.Create("FSharp", configuration)
//Taking input from the command lines ------->
//Argument 1 - Total number of nodes ------->
let numNodes = int fsi.CommandLineArgs.[1]
//Argument 2 - Number of Request to be sent ------->
let numReq = int fsi.CommandLineArgs.[2]
//Calculating the value of m from numNodes------->
let m = int (ceil(Math.Log(float(numNodes)) / Math.Log(2.0)))
//The flag that checks if all nodes have finished gossiping----->
let mutable endFlag = false
//The variable that stores the number of total hops------->
let mutable hops : int = 0
//The variable that stores the AVERAGE number of hops------->
let mutable avgHops : float = 0.0


//Create hash for nodes and keys ------->
let createHash (s3 : string) : string =  
    
    //printfn"Random String %s"s3
    let hash2 = 
       s3
       |> Encoding.ASCII.GetBytes
       |> (new SHA1Managed()).ComputeHash
       |> System.BitConverter.ToString

    let finhash = hash2.Replace("-","")
    printfn"Hash: %s" finhash
    finhash


//Defining the types of messages for supervisor actor-------->
type receivedSuperMessage =
    | StartP2P of int * int
    | DoneP2P of string

//Defining the types of messages for worker actors-------->
type receivedWorkerMessage = 
    | StartKeyLookup of int * int

//Selecting the random neighbor of an actor------->
let randNumGenerator (length:int) : int = 
    let r = System.Random()
    let num = r.Next(length)
    num

let rand = System.Random()
//The list that stores the Active Nodes------->
let ActiveNode = new List<int>()
//The list that stores nodes with their predecessor-------->
let predecessorTable = new Dictionary<int, string>()
//The list that stores nodes with their successor-------->
let successorTable = new Dictionary<int, string>()
//The map that stores fingers corresponding each node-------->
let fingerTable = new Dictionary<int, List<int>>()
//The table that stores successor node for each key------->
let keyTable = new Dictionary<int, int>()
let numnodes = numNodes * 2
//The list of all the keys------->
let keyList = new List<int>()
let time = Math.Log(float(numNodes)) / Math.Log(2.0) * float 950 + rand.NextDouble()


let mutable j = 0
let mutable num = rand.Next(numNodes)


let createKeyList () =    
    let mutable j = 0
    let mutable num = rand.Next(numNodes)
    while j < numReq do
        while(keyList.Contains(num)) do
            num <- rand.Next(numNodes)
        keyList.Add(num)
        j <- j + 1
    keyList.Sort()
()


let closestPrecedingNode (nodeExist : int) (nodeId : int) = 
    let mutable cpf : int = 0
    let fingerList = fingerTable.Item(nodeExist)
    let m' = fingerList.Count
    for i = m' - 1 downto 0 do
        if(fingerList.Item(i) > nodeExist && fingerList.Item(i) < pown 2 m || fingerList.Item(i) >= 0 && fingerList.Item(i) < nodeId) then
            cpf <- fingerList.Item(i)
    cpf
()


//The function that finds the successor of a given node------>
let rec findSuccessor (nodeExist : int) (nodeId : int) = 
    let mutable currSuccessor : int = 0
    let mutable n : int = 0
    let existSuccessor : int = int(successorTable.Item(nodeExist))
    if (nodeId > nodeExist && nodeId <= existSuccessor) then
        currSuccessor <- existSuccessor

    elif (existSuccessor < nodeExist && (nodeId <= existSuccessor || nodeId > nodeExist)) then 
        currSuccessor <- existSuccessor

    else
        n <- closestPrecedingNode nodeExist nodeId
        currSuccessor <- findSuccessor n nodeId
        //printfn "Successor of %d is - %d" nodeId currSuccessor
    currSuccessor
()


let closestPrecedingKeyNode (keyId : int) (nodeExist : int) = 
    let mutable cpf : int = 0
    let fingerList = fingerTable.Item(nodeExist)
    let m' = fingerList.Count
    for i = m' - 1 downto 0 do
        if(fingerList.Item(i) < keyId && keyId < pown 2 m || keyId >= 0 && nodeExist > keyId) then
            cpf <- fingerList.Item(i)
    cpf
()


//The function that finds the successor of a given key------>
let rec findKeySuccessor (keyId : int) (nodeId : int) = 
    let mutable currSuccessor : int = 0
    let mutable n : int = 0
    //let mutable h = 0
    let existSuccessor : int = int(successorTable.Item(nodeId))
    if (keyId > nodeId && keyId <= existSuccessor) then
        currSuccessor <- existSuccessor
    elif (keyId = nodeId) then 
        currSuccessor <- nodeId 
    elif (existSuccessor < nodeId && (keyId <= existSuccessor || nodeId < keyId)) then //CHANGED > TO <
        currSuccessor <- existSuccessor
    else
        n <- closestPrecedingKeyNode keyId nodeId
        currSuccessor <- findKeySuccessor keyId n
    currSuccessor
()


let cpnKey (keyId : int) (nodeExist : int) = 
    let mutable cpf : int = 0
    let fingerList = fingerTable.Item(nodeExist)
    let m' = fingerList.Count
    for i = m' - 1 downto 0 do
        if(fingerList.Item(i) < keyId && keyId < pown 2 m || keyId >= 0 && nodeExist > keyId) then
            hops <- hops + 1
            cpf <- fingerList.Item(i)
    cpf
()


//The function that finds the successor of a given key------>
let rec findKey (keyId : int) (nodeId : int) = 
    let mutable currSuccessor : int = 0
    let mutable n : int = 0
    //let mutable h = 0
    let existSuccessor : int = int(successorTable.Item(nodeId))
    if (keyId > nodeId && keyId <= existSuccessor) then
        currSuccessor <- existSuccessor
    elif (keyId = nodeId) then 
        currSuccessor <- nodeId 
    elif (existSuccessor < nodeId && (keyId <= existSuccessor || nodeId < keyId)) then //CHANGED > TO <
        currSuccessor <- existSuccessor
    else
        n <- cpnKey keyId nodeId
        currSuccessor <- findKey keyId n
    currSuccessor
()


let createKeyTable (nodeId : int) = 
    createKeyList()
    for i = 0 to keyList.Count - 1 do
        let keyId = keyList.Item(i)
        keyTable.Add(keyId, nodeId)
()


let updateKeyTable () = 
    for i = 0 to (keyList.Count - 1) do
        let keyId = keyList.Item(i)
        let id = 0
        let suc : int = findKeySuccessor keyId id
        keyTable.Remove(keyId) |> ignore
        keyTable.Add(keyId, suc)
()


//create the finger table for each actor----->
let createFingerTable (nodeId : int) = 
    let fingerList = new List<int>()
    let suc : int = int (successorTable.Item(nodeId))
    fingerList.Add(suc)
    for i = 1 to m do
        let finger = nodeId + pown 2 (i - 1) % pown 2 m
        if ActiveNode.Contains(finger) then 
            fingerList.Add(nodeId + finger)
    fingerTable.Add(nodeId, fingerList)
()

//update the finger table after each add of node----->
let updateFingerTable () = 
    for i = 0 to ActiveNode.Count - 1 do
        let fingerList = new List<int>()
        let nodeId = ActiveNode.Item(i)
        let suc : int = int (successorTable.Item(nodeId))
        fingerList.Add(suc)
        for i = 1 to m do
            let finger = nodeId + pown 2 (i - 1) % pown 2 m
            if ActiveNode.Contains(finger) then 
                fingerList.Add(nodeId + finger)
        fingerTable.Remove(nodeId) |> ignore
        fingerTable.Add(nodeId, fingerList)
()


let updateSuccessor ()= 
    ActiveNode.Sort()
    for i = 0 to ActiveNode.Count - 1 do
        //Checking if the successor of ith node in the ActiveList is (i+1)th node or not
        //If not then remove the successor from successor table
        //And then add the correct successor pair
        let a : int = int (successorTable.Item(ActiveNode.Item(i)))
        let mutable next = -1
        if i + 1 >= ActiveNode.Count then next <- 0 else next <- i + 1
        if ActiveNode.Item(next) <> a then
            successorTable.Remove(ActiveNode.Item(i)) |> ignore
            successorTable.Add(ActiveNode.Item(i), string(ActiveNode.Item(i + 1)))
()

let newlist = new List<int>()

//The worker function for each actor denoted by a node------>
let Node id (mailbox: Actor<_>) =
    //
    let rec nodeLoop (keySearched : List<int>) () =
        actor {
            //Receive message from the actor's inbox------->
            let! receivedWorkerMessage = mailbox.Receive()
            //Make the actor do work by matching the type of message------->
            let keySearchedUpdated = keySearched
            let mutable reqCount =  keySearched.Count
            match receivedWorkerMessage with
            | StartKeyLookup (k, nodeId) ->
                keySearchedUpdated.Add(k)
                if (reqCount < numReq - 1) then 
                    //Find the key in the ring----->
                    let b = findKey k nodeId
                    //Find another key k' from key list
                    let mutable randKey = randNumGenerator(numReq)
                    while(keySearchedUpdated.Contains(randKey)) do
                        randKey <- randNumGenerator(numReq)
                    //Search the key k' in the ring------>
                    WholeSystem.ActorSelection(String.Concat("user/Node", nodeId))
                    <! StartKeyLookup(randKey, nodeId)
                
                else
                    //printfn "Node finished: %d" id
                    WholeSystem.ActorSelection("user/Supervisor")
                    <! DoneP2P("Done")
            
            //| _ -> failwith "fail from node"

            return! nodeLoop keySearchedUpdated ()
        }
    nodeLoop newlist ()


//If it is the first node
//Then create the ring by adding the first node
//Setting the predecessor to null
//And successor to itself
let createRing (nodeId : int) = 
    ActiveNode.Add(nodeId)
    predecessorTable.Add(nodeId, "null")
    let id : string = string nodeId
    successorTable.Add(nodeId, id)
    createFingerTable nodeId
    printfn "Ring created."
    //printfn "Node %d joined." nodeId
    createKeyTable (nodeId)
()


//If it is the second node------->
//Then add it to the Activelist------->
//Update its successor------->
//Then ask the updateFingerTable to update the finger table of every node------>
let joinSecond (nodeExist : int) (nodeId : int) = 
    ActiveNode.Add(nodeId)
    let z : string = string (nodeExist)
    predecessorTable.Add(nodeId, z)
    successorTable.Add(nodeId, z)
    updateSuccessor ()
    updateFingerTable ()
    updateKeyTable ()
    //printfn "Node %d joined." nodeId
()


//If it is a node other than first and second node, 
//Join the node
//Update the SuccessorTable for each node
//Update the Finger Table for each node
let joinRing (nodeExist : int) (nodeId : int) = 
    ActiveNode.Add(nodeId) 
    let successor = findSuccessor nodeExist nodeId
    let id : string = string successor
    successorTable.Add(nodeId, id)
    predecessorTable.Add(nodeId, "null")
    createFingerTable nodeId
    updateSuccessor ()
    updateFingerTable ()
    updateKeyTable ()
    //printfn "Node %d joined." nodeId
()


//The function that adds the Actor to the ring------>
let addNode (nodeId : int) =
    let len = ActiveNode.Count
    if len = 0 then
        createRing nodeId
    elif len = 1 then
        joinSecond 0 nodeId
    else
        let id = 0
        joinRing id nodeId
()


//The worker 
let Supervisor =
    spawn WholeSystem "Supervisor"
    <| fun mailbox ->
        let rec SupervisorLoop counter () =
            actor {
                let! receivedSuperMessage = mailbox.Receive()
                match receivedSuperMessage with
                //Match received message with 
                | StartP2P (numNodes, numReq) ->
                    let rand = System.Random()
                    let mutable j = 0
                    let mutable num = 0
                    //Start by adding node 0 to the list-------->
                    for j = 0 to numNodes - 1 do
                        while(ActiveNode.Contains(num)) do
                            num <-  rand.Next(numNodes)
                        addNode(num)
                    //Creating a list for all the nodes there are in the system-------->
                    let nodeList =
                        [0..numNodes] |> List.map (fun id -> spawn WholeSystem (string id) (Node id))
                    let first = ActiveNode.Item(0)
                    let key = randNumGenerator (keyList.Count)
                    for i = 0 to numNodes - 1 do
                        let key' = randNumGenerator (keyList.Count)
                        nodeList.Item(i) <! StartKeyLookup (key', first)
                    
                //Check if all the nodes have done gossiping-------->
                | DoneP2P (someMsg) ->
                    if counter = numNodes - 1 then
                        printfn "All nodes converged. \n ------Exiting----------"
                        
                        //set the end flag to true------->
                        endFlag <- true

                //| _ -> failwith "unknown message"

                return! SupervisorLoop (counter + 1) ()

            }

        SupervisorLoop 0 ()


let startTime = DateTime.Now
let timer = Diagnostics.Stopwatch.StartNew()

Supervisor
<! StartP2P (numNodes, numReq)

//if end flag is still false,
//then continue with the process--------->
while not endFlag && (((DateTime.Now - startTime)).TotalMinutes < 0.10) do
    ignore ()

avgHops <- float (hops / numNodes)
printfn "Average Hops = %f " avgHops
timer.Stop()
printfn "Time taken for Convergence: %f milliseconds" time