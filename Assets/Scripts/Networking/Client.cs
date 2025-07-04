﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Debug = UnityEngine.Debug;

using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Unity.Mathematics;

public class Client
{
	// Internet Objects
	private Socket socket;
	private SocketError err;
	private DateTime lastMessageTime;
	private int timeoutSeconds = 5;

	// Entity Handler
	public EntityHandler entityHandler;
	public SmoothMovement smoothMovement;

	// Data Management
	private const int sendBufferSize = 4096; // 4KB
	private const int receiveBufferSize = 4096*4096; // 2MB
	private byte[] receiveBuffer;
	public List<NetMessage> queue = new List<NetMessage>();

	// Packet Management
	private bool lengthPacket = true;
	private int packetIndex = 0;
	private int packetSize = 0;
	private byte[] dataBuffer;

	// Address Information
	public IPAddress ip = new IPAddress(0x0F00A8C0);
	public int port = 33000;

	// Unity References
	public ChunkLoader cl;
	public PlayerRaycast raycast;
	public PlayerEvents playerEvents;
	public PlayerModelHandler playerModelHandler;

	// Const values
	private static int maxBufferSize = 327680;


	
	public Client(ChunkLoader cl){
		this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
		this.cl = cl;

		this.entityHandler = new EntityHandler(cl);
		this.smoothMovement = new SmoothMovement(this.entityHandler);
		
		receiveBuffer = new byte[receiveBufferSize];

		this.ip = World.connectionIP;

		this.Connect();
	}

	// Triggers hazard protection and sends user back to menu screen
	public void Panic(){
		Debug.Log("Panic");
        SceneManager.LoadScene("Blank");
	}

	public void SetRaycast(PlayerRaycast raycast){
		this.raycast = raycast;
        this.raycast.SetFOV();
	}

	public void SetPlayerEvents(PlayerEvents events){
		this.playerEvents = events;
	}

	public void SetPlayerModelHandler(PlayerModelHandler handler){
		this.playerModelHandler = handler;
	}
	
	
	public void Connect(){
		try{
			this.socket.Connect(this.ip, this.port);
			Debug.Log("Connecting");
		} catch {
			Panic();
		}

		this.socket.BeginReceive(receiveBuffer, 0, 4, 0, out this.err, new AsyncCallback(ReceiveCallback), null);
	}

	// Receive call handling
	private void ReceiveCallback(IAsyncResult result){
		try{
			// Resets timeout timer
			this.lastMessageTime = DateTime.Now;

			int bytesReceived = this.socket.EndReceive(result);

			// If has received a length Packet
			if(this.lengthPacket){
				int size = NetDecoder.ReadInt(receiveBuffer, 0);

				// Ignores packets that are way too big
				if(size > Client.maxBufferSize){
					this.socket.BeginReceive(receiveBuffer, 0, size, 0, out this.err, new AsyncCallback(ReceiveCallback), null);
					return;
				}

				this.dataBuffer = new byte[size];
				this.packetSize = size;
				this.packetIndex = 0;
				this.lengthPacket = false;

				this.socket.BeginReceive(receiveBuffer, 0, size, 0, out this.err, new AsyncCallback(ReceiveCallback), null);
				return;
			}

			// If has received a segmented packet
			if(bytesReceived + this.packetIndex < this.packetSize){
				Array.Copy(receiveBuffer, 0, this.dataBuffer, this.packetIndex, bytesReceived);
				this.packetIndex = this.packetIndex + bytesReceived;
				this.socket.BeginReceive(receiveBuffer, 0, this.packetSize-this.packetIndex, 0, out this.err, new AsyncCallback(ReceiveCallback), null);
				return;
			}

			Array.Copy(receiveBuffer, 0, this.dataBuffer, this.packetIndex, bytesReceived);

			NetMessage.Broadcast(NetBroadcast.RECEIVED, dataBuffer[0], 0, this.packetSize);

			NetMessage receivedMessage = new NetMessage(this.dataBuffer, 0);
			this.queue.Add(receivedMessage);

			this.lengthPacket = true;
			this.packetSize = 0;
			this.packetIndex = 0;

			this.socket.BeginReceive(receiveBuffer, 0, 4, 0, out this.err, new AsyncCallback(ReceiveCallback), null);
		}
		catch(Exception e){
			Debug.Log(e.ToString());
		}
	}

	// Sends a byte[] to the server
	public bool Send(NetMessage message){
		try{
			this.socket.Send(this.LengthPacket(message.size), 4, SocketFlags.None);
			this.socket.Send(message.GetMessage(), message.size, SocketFlags.None);
			return true;
		}
		catch(Exception e){
			Debug.Log("SEND ERROR: " + e.ToString());
			return false;
		}
	}

	// Checks if timeout has occured on server
	public void CheckTimeout(){
		if((DateTime.Now - this.lastMessageTime).Seconds > this.timeoutSeconds){
	        Debug.Log("Timeout");

	        Disconnect();
		}
	}

	/* 
	=========================================================================
	Handling of NetMessages
	*/

	// Discovers what to do with a Message received from Server
	public void HandleReceivedMessage(byte[] data){
		if(data == null)
			return;

		if(data.Length == 0)
			return;

		NetMessage.Broadcast(NetBroadcast.PROCESSED, data[0], 0, 0);

		switch((NetCode)data[0]){
			case NetCode.ACCEPTEDCONNECT:
				AcceptConnect();
				break;
			case NetCode.SENDSERVERINFO:
				SendServerInfo(data);
				break;
			case NetCode.SENDCHUNK:
				SendChunk(data);
				break;
			case NetCode.DISCONNECT:
				Disconnect();
				break;
			case NetCode.DIRECTBLOCKUPDATE:
				DirectBlockUpdate(data);
				break;
			case NetCode.VFXDATA:
				VFXData(data);
				break;
			case NetCode.VFXCHANGE:
				VFXChange(data);
				break;
			case NetCode.VFXBREAK:
				VFXBreak(data);
				break;
			case NetCode.SENDGAMETIME:
				SendGameTime(data);
				break;
			case NetCode.PLAYERLOCATION:
				PlayerLocation(data);
				break;
			case NetCode.SENDPLAYERAPPEARANCE:
				SendPlayerAppearance(data);
				break;
			case NetCode.SENDCHARSHEET:
				SendCharSheet(data);
				break;
			case NetCode.ENTITYDELETE:
				EntityDelete(data);
				break;
			case NetCode.PLACEMENTDENIED:
				PlacementDenied();
				break;
			case NetCode.ITEMENTITYDATA:
				ItemEntityData(data);
				break;
			case NetCode.BLOCKDAMAGE:
				BlockDamage(data);
				break;
			case NetCode.SENDINVENTORY:
				SendInventory(data);
				break;
			case NetCode.SFXPLAY:
				SFXPlay(data);
				break;
			case NetCode.SENDNOISE:
				SendNoise(data);
				break;
			case NetCode.SENDITEMINHAND:
				SendItemInHand(data);
				break;
			default:
				Debug.Log("UNKNOWN NETMESSAGE RECEIVED: " + (NetCode)data[0]);
				break;
		}
	}

	// Permits the loading of Game Scene
	private void AcceptConnect(){
		this.cl.CONNECTEDTOSERVER = true;
		Debug.Log("Connected to Server at " + this.socket.RemoteEndPoint.ToString());
	}

	// Receives Player Information saved on server on startup
	private void SendServerInfo(byte[] data){
		float x, y, z, xDir, yDir, zDir;
		uint day;
		byte hour, minute;
		CastCoord initialCoord;

		x = NetDecoder.ReadFloat(data, 1);
		y = NetDecoder.ReadFloat(data, 5);
		z = NetDecoder.ReadFloat(data, 9);
		xDir = NetDecoder.ReadFloat(data, 13);
		yDir = NetDecoder.ReadFloat(data, 17);
		zDir = NetDecoder.ReadFloat(data, 21);

		day = NetDecoder.ReadUint(data, 25);
		hour = data[29];
		minute = data[30];

		this.cl.time.SetTime(day, hour, minute);

		this.cl.PLAYERSPAWNED = true;
		this.cl.playerX = x;
		this.cl.playerY = y;
		this.cl.playerZ = z;
		this.cl.playerDirX = xDir;
		this.cl.playerDirY = yDir;
		this.cl.playerDirZ = zDir;

		// Finds current Chunk and sends position data
		initialCoord = new CastCoord(x, y, z);

		NetMessage message = new NetMessage(NetCode.REQUESTCHARACTERSHEET);
		message.RequestCharacterExistence(Configurations.accountID);
		this.Send(message);
	}

	// Receives a Chunk
	private void SendChunk(byte[] data){
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);

		// Returns index if exists and -1 otherwise
		int index = this.cl.toLoadChunk.IndexOf(pos);

		if(index == -1){
			this.cl.toLoad.Add(data);
			this.cl.toLoadChunk.Add(pos);
		}
		else{
			this.cl.toLoad[index] = data;
		}
	}

	// Receives a disconnect call from server
	private void Disconnect(){
		this.socket.Close();
		Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        this.cl.Cleanup(comesFromClient:true);
        SceneManager.LoadScene("Blank");
	}

	// Receives a Direct Block Update from server
	private void DirectBlockUpdate(byte[] data){
		ChunkPos pos;
		int x, y, z;
		ushort blockCode, state, hp;
		BUDCode type;

		pos = NetDecoder.ReadChunkPos(data, 1);
		x = NetDecoder.ReadInt(data, 10);
		y = NetDecoder.ReadInt(data, 14);
		z = NetDecoder.ReadInt(data, 18);
		int facing = NetDecoder.ReadInt(data, 22);

		blockCode = NetDecoder.ReadUshort(data, 26);
		state = NetDecoder.ReadUshort(data, 28);
		hp = NetDecoder.ReadUshort(data, 30);
		type = (BUDCode)NetDecoder.ReadInt(data, 32);

		switch(type){
			case BUDCode.PLACE:
				if(this.cl.Contains(pos)){
					this.cl.Get(pos).data.SetCell(x, y, z, blockCode);
					this.cl.Get(pos).metadata.SetState(x, y, z, state);
					this.cl.Get(pos).metadata.SetHP(x, y, z, hp);
					this.cl.Get(pos).data.CalculateHeightMap(x, z);
					this.cl.AddToUpdate(pos);
					CheckReload(pos, x, y, z);
				}
				break;
			case BUDCode.BREAK:
				if(this.cl.Contains(pos)){
					this.cl.Get(pos).data.SetCell(x, y, z, 0);
					this.cl.Get(pos).metadata.Reset(x,y,z);
					this.cl.Get(pos).data.CalculateHeightMap(x, z);
					this.cl.AddToUpdate(pos);
					CheckReload(pos, x, y, z);
				}	
				break;
			case BUDCode.CHANGE:
				if(this.cl.Contains(pos)){
					this.cl.Get(pos).data.SetCell(x, y, z, blockCode);
					this.cl.Get(pos).metadata.SetState(x, y, z, state);
					this.cl.Get(pos).metadata.SetHP(x, y, z, hp);
					this.cl.AddToUpdate(pos);
				}
				break;
			default:
				break;
		}
	}

	// Receives data on a VFX operation
	private void VFXData(byte[] data){
		ChunkPos pos;
		int x,y,z,facing;
		ushort blockCode, state;

		pos = NetDecoder.ReadChunkPos(data, 1);
		x = NetDecoder.ReadInt(data, 10);
		y = NetDecoder.ReadInt(data, 14);
		z = NetDecoder.ReadInt(data, 18);
		facing = NetDecoder.ReadInt(data, 22);
		blockCode = NetDecoder.ReadUshort(data, 26);
		state = NetDecoder.ReadUshort(data, 28);

		if(blockCode <= ushort.MaxValue/2){
			VoxelLoader.GetBlock(blockCode).OnVFXBuild(pos, x, y, z, facing, state, cl);
		}
		else{
			VoxelLoader.GetObject(blockCode).OnVFXBuild(pos, x, y, z, facing, state, cl);
		}
	}

	// Receives change in state of a VFX
	private void VFXChange(byte[] data){
		ChunkPos pos;
		int x,y,z,facing;
		ushort blockCode, state;

		pos = NetDecoder.ReadChunkPos(data, 1);
		x = NetDecoder.ReadInt(data, 10);
		y = NetDecoder.ReadInt(data, 14);
		z = NetDecoder.ReadInt(data, 18);
		facing = NetDecoder.ReadInt(data, 22);
		blockCode = NetDecoder.ReadUshort(data, 26);
		state = NetDecoder.ReadUshort(data, 28);

		if(blockCode <= ushort.MaxValue/2){
			VoxelLoader.GetBlock(blockCode).OnVFXChange(pos, x, y, z, facing, state, cl);
		}
		else{
			VoxelLoader.GetObject(blockCode).OnVFXChange(pos, x, y, z, facing, state, cl);
		}
	}

	// Receives a deletion of a VFX
	private void VFXBreak(byte[] data){
		ChunkPos pos;
		int x,y,z;
		ushort blockCode, state;

		pos = NetDecoder.ReadChunkPos(data, 1);
		x = NetDecoder.ReadInt(data, 10);
		y = NetDecoder.ReadInt(data, 14);
		z = NetDecoder.ReadInt(data, 18);
		blockCode = NetDecoder.ReadUshort(data, 22);
		state = NetDecoder.ReadUshort(data, 24);

		if(blockCode <= ushort.MaxValue/2){
			VoxelLoader.GetBlock(blockCode).OnVFXBreak(pos, x, y, z, state, cl);
		}
		else{
			VoxelLoader.GetObject(blockCode).OnVFXBreak(pos, x, y, z, state, cl);
		}
	}

	// Receives dd:hh:mm from server
	private void SendGameTime(byte[] data){
		uint days = NetDecoder.ReadUint(data, 1);
		byte hours = data[5];
		byte minutes = data[6];

		this.cl.time.SetTime(days, hours, minutes);
	}

	// Receives data regarding an entity
	private void PlayerLocation(byte[] data){
		ulong code = NetDecoder.ReadUlong(data, 1);
		float3 pos = NetDecoder.ReadFloat3(data, 9);
		float3 dir = NetDecoder.ReadFloat3(data, 21);

		if(this.entityHandler.Contains(EntityType.PLAYER, code)){
			this.entityHandler.NudgeLastPos(EntityType.PLAYER, code, pos, dir);
			this.smoothMovement.DefineMovement(EntityType.PLAYER, code, pos, dir);
		}
		else{
			this.entityHandler.AddPlayer(code, pos, dir);
			this.smoothMovement.AddPlayer(code);

			NetMessage charSheetMessage = new NetMessage(NetCode.REQUESTCHARACTERSHEET);
			charSheetMessage.RequestCharacterSheet(code);
			this.Send(charSheetMessage);

			NetMessage message = new NetMessage(NetCode.REQUESTPLAYERAPPEARANCE);
			message.RequestPlayerAppearance(code);
			this.Send(message);
		}
	}

	// Receives a player information
	private void SendPlayerAppearance(byte[] data){
		ulong code = NetDecoder.ReadUlong(data, 1);
		CharacterAppearance app = NetDecoder.ReadCharacterAppearance(data, 9);
		bool isMale = NetDecoder.ReadBool(data, 256);
		ushort item = NetDecoder.ReadUshort(data, 257);
		byte quantity = data[259];

		if(this.entityHandler.UpdatePlayerModel(code, app, isMale) && code != Configurations.accountID){
			this.entityHandler.UpdatePlayerItem(code, item, quantity);
		}
	}

	// Receives a character's sheet from Server
	private void SendCharSheet(byte[] data){
		ulong code = NetDecoder.ReadUlong(data, 1);
		CharacterSheet sheet = NetDecoder.ReadCharacterSheet(data, 9);

		if(code == Configurations.accountID){
			this.entityHandler.AddPlayerSheet(code, sheet);
			this.cl.playerSheetController.SetSheet(sheet);
			this.cl.playerEvents.ScrollToSlot(sheet.GetHotbarSlot());
		}
		else{
			this.entityHandler.AddPlayerSheet(code, sheet);			
		}
	}

	// Receives entity deletion command
	private void EntityDelete(byte[] data){
		EntityType type = (EntityType)data[1];
		ulong code = NetDecoder.ReadUlong(data, 2);

		this.entityHandler.Remove(type, code);
		this.smoothMovement.Remove(type, code);
	}


	// Returns a byte array representation of a int
	private byte[] LengthPacket(int a){
		byte[] output = new byte[4];

		output[0] = (byte)(a >> 24);
		output[1] = (byte)(a >> 16);
		output[2] = (byte)(a >> 8);
		output[3] = (byte)a;

		return output;
	}

	// Signals to a neighbor chunk to be reloaded after a BUD
	private void CheckReload(ChunkPos pos, int x, int y, int z){
		ChunkPos temp;

		if(x == 0){
			temp = new ChunkPos(pos.x-1, pos.z, pos.y);
			if(this.cl.Contains(temp))
				this.cl.AddToUpdate(temp);
		}

		if(x == Chunk.chunkWidth-1){
			temp = new ChunkPos(pos.x+1, pos.z, pos.y);
			if(this.cl.Contains(temp))
				this.cl.AddToUpdate(temp);
		}

		if(z == 0){
			temp = new ChunkPos(pos.x, pos.z-1, pos.y);
			if(this.cl.Contains(temp))
				this.cl.AddToUpdate(temp);			
		}

		if(z == Chunk.chunkWidth-1){
			temp = new ChunkPos(pos.x, pos.z+1, pos.y);
			if(this.cl.Contains(temp))
				this.cl.AddToUpdate(temp);
		}

		if(y == 0 && pos.y > 0){
			temp = new ChunkPos(pos.x, pos.z, pos.y-1);
			if(this.cl.Contains(temp))
				this.cl.AddToUpdate(temp);
		}

		if(y == Chunk.chunkDepth-1 && pos.y < Chunk.chunkMaxY){
			temp = new ChunkPos(pos.x, pos.z, pos.y+1);
			if(this.cl.Contains(temp))
				this.cl.AddToUpdate(temp);
		}

		if(x == 0 && z == 0){
			temp = new ChunkPos(pos.x-1, pos.z-1, pos.y);
			if(this.cl.Contains(temp))
				this.cl.AddToUpdate(temp);
		}

		if(x == Chunk.chunkWidth-1 && z == 0){
			temp = new ChunkPos(pos.x+1, pos.z-1, pos.y);
			if(this.cl.Contains(temp))
				this.cl.AddToUpdate(temp);
		}

		if(x == Chunk.chunkWidth-1 && z == Chunk.chunkWidth-1){
			temp = new ChunkPos(pos.x+1, pos.z+1, pos.y);
			if(this.cl.Contains(temp))
				this.cl.AddToUpdate(temp);
		}

		if(x == 0 && z == Chunk.chunkWidth-1){
			temp = new ChunkPos(pos.x-1, pos.z+1, pos.y);
			if(this.cl.Contains(temp))
				this.cl.AddToUpdate(temp);
		}
	}

	// Signals Raycast to giveback the last placed item
	private void PlacementDenied(){
		ItemStack its = new ItemStack(cl.playerRaycast.lastBlockPlaced, 1);
		this.raycast.playerEvents.hotbar.AddStack(its, this.raycast.playerEvents.hotbar.CanFit(its));
		this.raycast.playerEvents.DrawHotbar();
		this.raycast.playerEvents.invUIPlayer.SendInventoryDataToServer();
	}

	// Received information on an Item Entity
	private void ItemEntityData(byte[] data){
		ItemStack its;
		float3 pos, rot;
		ulong code;

		pos = NetDecoder.ReadFloat3(data, 1);
		rot = NetDecoder.ReadFloat3(data, 13);
		its = new ItemStack(NetDecoder.ReadUshort(data, 25), data[27]);
		code = NetDecoder.ReadUlong(data, 28);

		if(this.entityHandler.Contains(EntityType.DROP, code)){
			this.smoothMovement.DefineMovement(EntityType.DROP, code, pos, rot);
			this.entityHandler.NudgeLastPos(EntityType.DROP, code, pos, rot);
			this.entityHandler.ToggleItemAnimation(EntityType.DROP, code, rot.x);

			// Stop Signal
			if(rot.x == 1f){
				this.smoothMovement.StopEntity(EntityType.DROP, code);
				this.entityHandler.SetItemPosition(code, pos);
			}
		}
		else{
			this.entityHandler.AddItem(code, pos, rot, its);
			this.entityHandler.ToggleItemAnimation(EntityType.DROP, code, rot.x);
			this.smoothMovement.AddItem(code);
		}
	}

	// Receives block damage information from server
	private void BlockDamage(byte[] data){
		ChunkPos pos;
		int x,y,z;
		ushort newHP;
		bool shouldRedraw;
		Chunk c;

		pos = NetDecoder.ReadChunkPos(data, 1);
		x = NetDecoder.ReadInt(data, 10);
		y = NetDecoder.ReadInt(data, 14);
		z = NetDecoder.ReadInt(data, 18);
		newHP = NetDecoder.ReadUshort(data, 22);
		shouldRedraw = NetDecoder.ReadBool(data, 24);

		if(this.cl.Contains(pos)){
			c = this.cl.Get(pos);
			c.metadata.SetHP(x, y, z, newHP);

			if(shouldRedraw){
				this.cl.AddToUpdate(pos);
				CheckReload(pos, x, y, z);
			}
		}
	}

	// Receives player inventory information from server and builds into player inventory
	private void SendInventory(byte[] data){
		Inventory newInv = new Inventory(InventoryType.PLAYER);
		Inventory newHot = new Inventory(InventoryType.HOTBAR);

		InventorySerializer.BuildPlayerInventory(data, 1, out newHot, out newInv);

		if(this.playerEvents != null){
			this.playerEvents.SetInventories(newInv, newHot);
		}
		else{
			this.cl.playerEvents.SetInventories(newInv, newHot);
			this.playerEvents = this.cl.playerEvents;
		}
	}

	// Receives a request to register an SFX into SFXLoader
	private void SFXPlay(byte[] data){
		int x,y,z;
		ChunkPos pos = NetDecoder.ReadChunkPos(data, 1);
		x = NetDecoder.ReadInt(data, 10);
		y = NetDecoder.ReadInt(data, 14);
		z = NetDecoder.ReadInt(data, 18);
		ushort blockCode = NetDecoder.ReadUshort(data, 22);
		ushort state = NetDecoder.ReadUshort(data, 24);

		if(blockCode <= ushort.MaxValue/2){
			VoxelLoader.GetBlock(blockCode).OnSFXPlay(pos, x, y, z, state, cl);
		}
		else{
			VoxelLoader.GetObject(blockCode).OnSFXPlay(pos, x, y, z, state, cl);
		}
	}

	// Receives weather Noise information from client
	private void SendNoise(byte[] data){
		Array.Copy(GenerationSeed.weatherNoise, 0, data, 1, data.Length-5);
		int seed = NetDecoder.ReadInt(data, data.Length-4);
		World.worldSeed = seed;
	}

	// Receives the item code for an item a player is holding
	private void SendItemInHand(byte[] data){
		ulong playerCode = NetDecoder.ReadUlong(data, 1);
		ushort item = NetDecoder.ReadUshort(data, 9);
		byte amount = data[11];

		this.entityHandler.UpdatePlayerItem(playerCode, item, amount);
	}

	/* ================================================================================ */

	// Activates SmoothMovement in Entities for the current frame
	public void MoveEntities(){
		this.entityHandler.RunEntityActivation();
		this.smoothMovement.MoveEntities();
	}
}
