using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkPriorityQueue
{
    private List<ChunkDistance> initialQueue; // Queue that processes only prioritary elements
    private List<ChunkDistance> queue; // Second queue that handles normal execution
    private List<ChunkDistance> backupQueue; // Cache queue to recalculate distances

    private ChunkPos playerPosition; // Player current Distance

    public ChunkPriorityQueue(){
        this.queue = new List<ChunkDistance>();
        this.initialQueue = new List<ChunkDistance>();
    }

    public void Add(ChunkPos x, bool initial=false){
        int distance = playerPosition.DistanceFrom(x);
        int newDist = 0;

        List<ChunkDistance> q;

        if(initial)
            q = this.initialQueue;
        else
            q = this.queue;

        // Empty Queue
        if(q.Count == 0){
            q.Add(new ChunkDistance(x, distance));
            return;
        }

        for(int i=0; i < q.Count; i++){
            newDist = playerPosition.DistanceFrom(q[i].pos);
            if(distance < newDist){
                q.Insert(i, new ChunkDistance(x, distance));
                return;
            }
        }

        if(newDist == playerPosition.DistanceFrom(q[q.Count-1].pos))
            q.Add(new ChunkDistance(x, distance));
        else
            q.Insert(0, new ChunkDistance(x, distance));
    }

    public bool Contains(ChunkPos x){
        int distance = playerPosition.DistanceFrom(x);

        return this.queue.Contains(new ChunkDistance(x, distance)) || this.initialQueue.Contains(new ChunkDistance(x, distance));
    }

    public int GetSize(){
        return this.queue.Count + this.initialQueue.Count;
    }

    public ChunkPos Peek(){
        if(GetSize() > 0){
            if(this.initialQueue.Count > 0)
                return this.initialQueue[0].pos;
            else
                return this.queue[0].pos;
        }

        return playerPosition;
    }

    public ChunkPos Pop(){
        if(this.initialQueue.Count > 0){
            ChunkPos aux = this.initialQueue[0].pos;
            this.initialQueue.RemoveAt(0);
            return aux;
        }
        else{
            ChunkPos aux = this.queue[0].pos;
            this.queue.RemoveAt(0);
            return aux;            
        }

    }

    public void Remove(ChunkPos x){
        for(int i=0; i < this.initialQueue.Count; i++){
            if(this.initialQueue[i].pos == x){
                this.initialQueue.RemoveAt(i);
                return;
            }
        }

        for(int i=0; i < this.queue.Count; i++){
            if(this.queue[i].pos == x){
                this.queue.RemoveAt(i);
                return;
            }
        }        
    }

    public void Clear(){
        this.queue.Clear();
    }

    public void SetPlayerPosition(ChunkPos pos){
        this.playerPosition = pos;

        RenewDistances();
    }

    private void RenewDistances(){
        this.backupQueue = new List<ChunkDistance>(this.queue);
        this.queue.Clear();

        for(int i=0; i < this.backupQueue.Count; i++){
            Add(this.backupQueue[i].pos);
        }

        this.backupQueue.Clear();
    }

    private void Print(){
        string a = "[";

        for(int i=0; i < this.queue.Count; i++)
            a += this.queue[i].distance + ", ";

        a += "]";

        Debug.Log(a);
    }
}

public struct ChunkDistance{
    public ChunkPos pos;
    public int distance;

    public ChunkDistance(ChunkPos pos, int distance){
        this.pos = pos;
        this.distance = distance;
    }

    public override string ToString(){
        return this.pos.ToString() + " -> " + this.distance;
    }
}