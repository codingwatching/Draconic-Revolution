﻿using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Region Data File Format (.rdf)

| RDF File                                                                      |
| Chunk Header (21 bytes)  || Chunk Data (ChunkDimensions*8 bytes)                |
-> | Biome (1 byte) LastDay(4 bytes) LastHour(1 byte) LastMinute(1 byte) LastTick(1 byte) NeedGeneration (1 byte) |     -> | BlockData  (ChunkDimensions*4 bytes) || Metadata (ChunkDimensions*4 bytes) | 
-> | BlockDataSize (4 bytes) | HPDataSize (4 bytes) | StateDataSize (4 bytes)
*/

public class RegionFileHandler{
	private RegionFile region;
	private int seed;
	private int renderDistance;
	private static float chunkLength = 32f;
	public TimeOfDay globalTime;


	// Cache Information
	private byte[] nullMetadata = new byte[]{255,255};

	private byte[] byteArray = new byte[1];
	private byte[] timeArray = new byte[7];
	private byte[] indexArray = new byte[16];
	private byte[] headerBuffer = new byte[21];
	private byte[] blockBuffer = new byte[Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 4]; // Exagerated buffer (roughly 0,1 MB)
	private byte[] hpBuffer = new byte[Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 4]; // Exagerated buffer 
	private byte[] stateBuffer = new byte[Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 4]; // Exagerated buffer

	// Sizes
	private static int chunkHeaderSize = 21; // Size (in bytes) of header
	private static int chunkSize = Chunk.chunkWidth * Chunk.chunkWidth * Chunk.chunkDepth * 8; // Size in bytes of Chunk payload

	// Builds the file handler and loads it on the ChunkPos of the first player
	public RegionFileHandler(int seed, int renderDistance, ChunkPos pos){
		this.seed = seed;
		this.renderDistance = renderDistance;
		this.globalTime = GameObject.Find("/Time Counter").GetComponent<TimeOfDay>();

		LoadRegionFile(pos, init:true);
	}

	// Checks if RegionFile represents ChunkPos, and loads correct RegionFile if not
	public void GetCorrectRegion(ChunkPos pos){
		if(!region.CheckUsage(pos)){
			LoadRegionFile(pos);
		}
	}

	// Getter for RegionFile
	public RegionFile GetFile(){
		return this.region;
	}

	// Loads RegionFile related to given Chunk
	public void LoadRegionFile(ChunkPos pos, bool init=false){
		int rfx;
		int rfz;
		string name;

		rfx = Mathf.FloorToInt(pos.x / RegionFileHandler.chunkLength);
		rfz = Mathf.FloorToInt(pos.z / RegionFileHandler.chunkLength);
		name = "r" + rfx.ToString() + "x" + rfz.ToString();

		// Saves current RegionFile and open a new one
		if(!init){
			this.region.Close();
		}

		region = new RegionFile(name, new ChunkPos(rfx, rfz), RegionFileHandler.chunkLength);
	}

	// Loads a chunk information from RDF file using Pallete-based Decompression
	// Assumes that the correct Region File has been called
	public void LoadChunk(Chunk c){
		byte biome=0;
		byte gen=0;
		int blockdata=0;
		int hpdata=0;
		int statedata=0;

		ReadHeader(c.pos);
		InterpretHeader(ref biome, ref gen, ref blockdata, ref hpdata, ref statedata);

		c.biomeName = BiomeHandler.ByteToBiome(biome);
		c.lastVisitedTime = globalTime.DateBytes(timeArray);
		c.needsGeneration = gen;

		this.region.file.Read(blockBuffer, 0, blockdata);
		this.region.file.Read(hpBuffer, 0, hpdata);
		this.region.file.Read(stateBuffer, 0, statedata);

		Compression.DecompressBlocks(c, blockBuffer);
		Compression.DecompressMetadataHP(c, hpBuffer);
		Compression.DecompressMetadataState(c, stateBuffer);

	}

	// Saves a chunk to RDF file using Pallete-based Compression
	// Assumes that the correct Region File has been called
	public void SaveChunk(Chunk c){
		int totalSize = 0;
		long seekPosition = 0;
		int blockSize;
		int hpSize;
		int stateSize;
		long chunkCode = GetLinearRegionCoords(c.pos);
		int lastKnownSize=0;

		// Reads pre-save size if is already indexed
		if(region.IsIndexed(c.pos)){
			byte biome=0;
			byte gen=0;
			int blockdata=0;
			int hpdata=0;
			int statedata=0;

			ReadHeader(c.pos);
			InterpretHeader(ref biome, ref gen, ref blockdata, ref hpdata, ref statedata);
			lastKnownSize = chunkHeaderSize + blockdata + hpdata + statedata;
		}


		// Saves data to buffers and gets total size
		blockSize = Compression.CompressBlocks(c, blockBuffer);
		hpSize = Compression.CompressMetadataHP(c, hpBuffer);
		stateSize = Compression.CompressMetadataState(c, stateBuffer);

		InitializeHeader(c, blockSize, hpSize, stateSize);

		totalSize = chunkHeaderSize + blockSize + hpSize + stateSize;

		// If Chunk was already saved
		if(region.IsIndexed(c.pos)){
			region.AddHole(region.index[chunkCode], lastKnownSize);
			seekPosition = region.FindPosition(totalSize);
			region.SaveHoles();

			// If position in RegionFile has changed
			if(seekPosition != region.index[chunkCode]){
				region.index[chunkCode] = seekPosition;
				region.UnloadIndex();
			}

			// Saves Chunk
			region.Write(seekPosition, headerBuffer, chunkHeaderSize);
			region.Write(seekPosition+chunkHeaderSize, blockBuffer, blockSize);
			region.Write(seekPosition+chunkHeaderSize+blockSize, hpBuffer, hpSize);
			region.Write(seekPosition+chunkHeaderSize+blockSize+hpSize, stateBuffer, stateSize);
		}
		// If it's a new Chunk
		else{
			seekPosition = region.FindPosition(totalSize);
			region.SaveHoles();

			// Adds new chunk to Index
			region.index.Add(chunkCode, seekPosition);
			AddEntryIndex(chunkCode, seekPosition);
			region.indexFile.Write(indexArray, 0, 16);
			region.indexFile.Flush();

			// Saves Chunk
			region.Write(seekPosition, headerBuffer, chunkHeaderSize);
			region.Write(seekPosition+chunkHeaderSize, blockBuffer, blockSize);
			region.Write(seekPosition+chunkHeaderSize+blockSize, hpBuffer, hpSize);
			region.Write(seekPosition+chunkHeaderSize+blockSize+hpSize, stateBuffer, stateSize);
		}

	}

	// Reads header from a chunk
	// leaves rdf file at Seek Position ready to read blockdata
	private void ReadHeader(ChunkPos pos){
		long code = GetLinearRegionCoords(pos);

		this.region.file.Seek(this.region.index[code], SeekOrigin.Begin);
		this.region.file.Read(headerBuffer, 0, chunkHeaderSize);
	}

	// Interprets header data into ref variables
	private void InterpretHeader(ref byte biome, ref byte gen, ref int blockdata, ref int hpdata, ref int statedata){
		biome = headerBuffer[0];

		timeArray[0] = headerBuffer[1];
		timeArray[1] = headerBuffer[2];
		timeArray[2] = headerBuffer[3];
		timeArray[3] = headerBuffer[4];
		timeArray[4] = headerBuffer[5];
		timeArray[5] = headerBuffer[6];
		timeArray[6] = headerBuffer[7];

		gen = headerBuffer[8];

		blockdata = headerBuffer[9];
		blockdata = blockdata << 8;
		blockdata += headerBuffer[10];
		blockdata = blockdata << 8;
		blockdata += headerBuffer[11];
		blockdata = blockdata << 8;
		blockdata += headerBuffer[12];

		hpdata = headerBuffer[13];
		hpdata = hpdata << 8;
		hpdata += headerBuffer[14];
		hpdata = hpdata << 8;
		hpdata += headerBuffer[15];
		hpdata = hpdata << 8;
		hpdata += headerBuffer[16];

		statedata = headerBuffer[17];
		statedata = statedata << 8;
		statedata += headerBuffer[18];
		statedata = statedata << 8;
		statedata += headerBuffer[19];
		statedata = statedata << 8;
		statedata += headerBuffer[20];
	}

	// Writes Chunk Header to headerBuffer
	private void InitializeHeader(Chunk c, int blockSize, int hpSize, int stateSize){
		timeArray = globalTime.TimeHeader();

		headerBuffer[0] = BiomeHandler.BiomeToByte(c.biomeName);		

		for(int i=0; i<7; i++){
			headerBuffer[i+1] = timeArray[i];
		}

		headerBuffer[8] = c.needsGeneration;

		headerBuffer[9] = (byte)(blockSize >> 24);
		headerBuffer[10] = (byte)(blockSize >> 16);
		headerBuffer[11] = (byte)(blockSize >> 8);
		headerBuffer[12] = (byte)(blockSize);

		headerBuffer[13] = (byte)(hpSize >> 24);
		headerBuffer[14] = (byte)(hpSize >> 16);
		headerBuffer[15] = (byte)(hpSize >> 8);
		headerBuffer[16] = (byte)(hpSize);

		headerBuffer[17] = (byte)(stateSize >> 24);
		headerBuffer[18] = (byte)(stateSize >> 16);
		headerBuffer[19] = (byte)(stateSize >> 8);
		headerBuffer[20] = (byte)(stateSize);
	}

	// Quick Saves a new entry to index file
	private void AddEntryIndex(long key, long val){
		indexArray[0] = (byte)(key >> 56);
		indexArray[1] = (byte)(key >> 48);
		indexArray[2] = (byte)(key >> 40);
		indexArray[3] = (byte)(key >> 32);
		indexArray[4] = (byte)(key >> 24);
		indexArray[5] = (byte)(key >> 16);
		indexArray[6] = (byte)(key >> 8);
		indexArray[7] = (byte)(key);
		indexArray[8] = (byte)(val >> 56);
		indexArray[9] = (byte)(val >> 48);
		indexArray[10] = (byte)(val >> 40);
		indexArray[11] = (byte)(val >> 32);
		indexArray[12] = (byte)(val >> 24);
		indexArray[13] = (byte)(val >> 16);
		indexArray[14] = (byte)(val >> 8);
		indexArray[15] = (byte)(val);
	}

	// Gets NeedGeneration byte from Chunk
	public bool GetsNeedGeneration(ChunkPos pos){
		ReadHeader(pos);

		if(headerBuffer[8] == 0)
			return false;
		return true;
	}


	// Convert to linear Region Chunk Coordinates
	private long GetLinearRegionCoords(ChunkPos pos){
		return (long)(pos.z*chunkLength + pos.x);
	}

}

public struct RegionFile{
	public string name;
	public ChunkPos regionPos; // Variable to represent Region coordinates, and not Chunk coordinates
	private float chunkLength;

	// File Data
	public Stream file;
	public Stream indexFile;
	public Stream holeFile;
	public Dictionary<long, long> index;
	public FragmentationHandler fragHandler;

	// Cached Data
	private byte[] cachedIndex;
	private byte[] longArray;
	private byte[] cachedHoles;

	// Opens the file and adds ".rdf" at the end (Region Data File)
	public RegionFile(string name, ChunkPos pos, float chunkLen){
		bool isLoaded = true;

		this.name = name;
		this.regionPos = pos;
		this.chunkLength = chunkLen;
		this.index = new Dictionary<long, long>();

		this.cachedIndex = new byte[16384];
		this.cachedHoles = new byte[16384];
		this.longArray = new byte[8];

		try{
			this.file = File.Open(name + ".rdf", FileMode.Open);
		} 
		catch (FileNotFoundException){
			isLoaded = false;
			this.file = File.Open(name + ".rdf", FileMode.Create);
		}

		try{
			this.indexFile = File.Open(name + ".ind", FileMode.Open);
		} 
		catch (FileNotFoundException){
			isLoaded = false;
			this.indexFile = File.Open(name + ".ind", FileMode.Create);
		}

		try{
			this.holeFile = File.Open(name + ".hle", FileMode.Open);
		} 
		catch (FileNotFoundException){
			isLoaded = false;
			this.holeFile = File.Open(name + ".hle", FileMode.Create);
		}

		this.fragHandler = new FragmentationHandler(isLoaded);
		
		if(isLoaded){
			LoadIndex();
			LoadHoles();
		}
	}

	// Checks if current chunk should be housed in current RegionFile
	public bool CheckUsage(ChunkPos pos){
		int rfx;
		int rfz;

		rfx = Mathf.FloorToInt(pos.x / this.chunkLength);
		rfz = Mathf.FloorToInt(pos.z / this.chunkLength);

		if(this.regionPos.x == rfx && this.regionPos.z == rfz)
			return true;
		return false;
	}

	// Checks if current chunk is in index already
	public bool IsIndexed(ChunkPos pos){
		if(index.ContainsKey(GetLinearRegionCoords(pos)))
			return true;
		return false;
	}

	// Convert to linear Region Chunk Coordinates
	private long GetLinearRegionCoords(ChunkPos pos){
		return (long)(pos.z*chunkLength + pos.x);
	}

	// Overload?
	public void AddHole(long pos, int size, bool infinite=false){
		this.fragHandler.AddHole(pos, size, infinite:infinite);
	}

	// Overload?
	public long FindPosition(int size){
		return this.fragHandler.FindPosition(size);
	}

	// Writes buffer stream to file
	public void Write(long position, byte[] buffer, int size){
		this.file.Seek(position, SeekOrigin.Begin);
		this.file.Write(buffer, 0, size);
		this.file.Flush();
	}

	// Reads index data com IND file
	public void LoadIndex(){
		this.indexFile.Seek(0, SeekOrigin.Begin);

		ReadIndexEntry();
		long a;
		long b;

		for(int i=0; i<this.indexFile.Length; i+=16){
			a = ReadLong(i);
			b = ReadLong(i+8);

			this.index[a] = b;
		}
	}

	// Save hole data to the HLE file
	public void SaveHoles(){
		bool done = false;
		int offset = 0;
		int writtenBytes = 0;

		this.holeFile.SetLength(0);
		writtenBytes = this.fragHandler.CacheHoles(offset, ref done);
		this.holeFile.Write(this.fragHandler.cachedHoles, 0, writtenBytes);
		this.holeFile.Flush();

		while(!done){
			offset++;
			writtenBytes = this.fragHandler.CacheHoles(offset, ref done);
			this.holeFile.Write(this.fragHandler.cachedHoles, 0, writtenBytes);
			this.holeFile.Flush();		
		}
	}

	// Loads all DataHole data to Fragment Handlers list
	public void LoadHoles(){
		this.holeFile.Seek(0, SeekOrigin.Begin);

		if(this.holeFile.Length <= 16380){
			this.holeFile.Read(this.cachedHoles, 0, (int)holeFile.Length);
			AddHolesFromBuffer((int)holeFile.Length);
		}
		else{
			int times = 0;

			this.holeFile.Read(this.cachedHoles, 0, 16380);
			AddHolesFromBuffer(16380);
			times++;

			// While there is still data to be read
			while(holeFile.Length - times*16380 >= 16380){
				this.holeFile.Read(this.cachedHoles, 0, 16380);
				AddHolesFromBuffer(16380);
				times++;
			}

			// Reads remnants
			if(holeFile.Length - times*16380 > 0){
				this.holeFile.Read(this.cachedHoles, 0, (int)(holeFile.Length - times*16380));
				AddHolesFromBuffer((int)(holeFile.Length - times*16380));
			}

		}
	}

	// Adds holes read from buffer data
	private void AddHolesFromBuffer(int readBytes){
		long a;
		int b;

		for(int i=0; i < readBytes; i+= 12){
			a = ReadLongHole(i);
			b = ReadIntHole(i+8);

			if(b > 0){
				AddHole(a, b);
			}
			else{
				AddHole(a, -1, infinite:true);
			}
		}
	}

	// Writes all index data to index file
	public void UnloadIndex(){
		int position = 0;

		foreach(long l in this.index.Keys){
			ReadIndexLong(l, position);
			position += 8;
			ReadIndexLong(this.index[l], position);
			position += 8;
		}

		this.indexFile.SetLength(0);
		this.indexFile.Write(this.cachedIndex, 0, position);
		this.indexFile.Flush();
	}

	// Reads all index file and sends it to cachedIndex
	private void ReadIndexEntry(){
		this.indexFile.Read(this.cachedIndex, 0, (int)this.indexFile.Length);
	}

	// Reads a long in byte[] cachedIndex at position n
	private long ReadLong(int pos){
		long a;

		a = cachedIndex[pos];
		a = a << 8;
		a += cachedIndex[pos+1];
		a = a << 8;
		a += cachedIndex[pos+2];
		a = a << 8;
		a += cachedIndex[pos+3];
		a = a << 8;
		a += cachedIndex[pos+4];
		a = a << 8;
		a += cachedIndex[pos+5];
		a = a << 8;
		a += cachedIndex[pos+6];
		a = a << 8;
		a += cachedIndex[pos+7];

		return a;
	}

	// Reads a long in byte[] cachedHoles at position n
	private long ReadLongHole(int pos){
		long a;

		a = this.cachedHoles[pos];
		a = a << 8;
		a += this.cachedHoles[pos+1];
		a = a << 8;
		a += this.cachedHoles[pos+2];
		a = a << 8;
		a += this.cachedHoles[pos+3];
		a = a << 8;
		a += this.cachedHoles[pos+4];
		a = a << 8;
		a += this.cachedHoles[pos+5];
		a = a << 8;
		a += this.cachedHoles[pos+6];
		a = a << 8;
		a += this.cachedHoles[pos+7];

		return a;
	}

	// Reads an int in byte[] cachedIndex at position n
	private int ReadInt(int pos){
		int a;

		a = cachedIndex[pos];
		a = a << 8;
		a += cachedIndex[pos+1];
		a = a << 8;
		a += cachedIndex[pos+2];
		a = a << 8;
		a += cachedIndex[pos+3];

		return a;
	}

	// Reads an int in byte[] cachedHoles at position n
	private int ReadIntHole(int pos){
		int a;

		a = this.cachedHoles[pos];
		a = a << 8;
		a += this.cachedHoles[pos+1];
		a = a << 8;
		a += this.cachedHoles[pos+2];
		a = a << 8;
		a += this.cachedHoles[pos+3];

		return a;
	}

	// Adds a long to cached byte array of index
	private void ReadIndexLong(long l, int position){
		this.cachedIndex[position] = (byte)(l >> 56);
		this.cachedIndex[position+1] = (byte)(l >> 48);
		this.cachedIndex[position+2] = (byte)(l >> 40);
		this.cachedIndex[position+3] = (byte)(l >> 32);
		this.cachedIndex[position+4] = (byte)(l >> 24);
		this.cachedIndex[position+5] = (byte)(l >> 16);
		this.cachedIndex[position+6] = (byte)(l >> 8);
		this.cachedIndex[position+7] = (byte)l;
	}

	// Closes all Streams
	public void Close(){
		UnloadIndex();
		SaveHoles();

		this.file.Close();
		this.indexFile.Close();
		this.holeFile.Close();
	}

}


/*
Handles DataHoles and makes sure there's little fragmentation to disk
*/
public class FragmentationHandler{
	private List<DataHole> data;
	public byte[] cachedHoles = new byte[384]; // 32 Holes per Read

	public FragmentationHandler(bool loaded){
		this.data = new List<DataHole>(){};

		if(!loaded)
			this.data.Add(new DataHole(0, -1, infinite:true));
	}

	// Finds a position in RegionFile that fits
	// a chunk with given size
	public long FindPosition(int size){
		long output;

		for(int i=0; i < this.data.Count; i++){
			if(data[i].size > size){
				output = data[i].position;
				data.Insert(i+1, new DataHole(data[i].position + size, (int)data[i].size - size));
				data.RemoveAt(i);
				return output;
			}
			else if(data[i].size == size){
				output = data[i].position;
				data.RemoveAt(i);
				return output;				
			}
		}

		output = data[data.Count-1].position;
		data.Add(new DataHole(data[data.Count-1].position + size, -1, infinite:true));
		data.RemoveAt(data.Count-2);
		return output;
	}

	// Puts hole data in CachedHoles
	// Returns the amount of bytes written and a reference bool that serves as a flag
	// When the flag is true, caching has been completed. If false, more CacheHoles need to be called
	// Offset is a multiplier of 384 indices
	public int CacheHoles(int offset, ref bool done){
		done = this.data.Count - offset*32 <= 32;
		int index=0;

		for(int i=offset*32; i < this.data.Count; i++){
			data[i].Bytefy(this.cachedHoles, index);
			index += 12;
		}
		return index;
	}

	// Adds a DataHole to list in a priority list fashion
	public void AddHole(long pos, int size, bool infinite=false){
		if(infinite){
			this.data.Add(new DataHole(pos, -1, infinite:true));
			return;
		}

		for(int i=0; i<this.data.Count;i++){
			if(this.data[i].position > pos){
				this.data.Insert(i, new DataHole(pos, size));
				MergeHoles(i);
				return;
			}
		}

		// Adds a hole if there isn't any
		this.data.Add(new DataHole(pos, size));
		return;
	}

	// Removes if hole has no size
	private bool RemoveZero(DataHole dh){
		if(dh.size == 0){
			this.data.Remove(dh);
			return true;
		}
		return false;
	}

	public int Count(){
		return this.data.Count;
	}

	// Merges DataHoles starting from pos in data list if there's any
	// ONLY USE WHEN JUST ADDED A HOLE IN POS
	private void MergeHoles(int index){
		if(this.data[index].position + this.data[index].size == this.data[index+1].position){
			
			// If neighbor hole is infinite
			if(this.data[index+1].infinite){
				this.data.RemoveAt(index+1);
				this.data[index] = new DataHole(this.data[index].position, -1, infinite:true);
			}
			// If neighbor is a normal hole
			else{
				this.data[index] = new DataHole(this.data[index].position, this.data[index].size + this.data[index+1].size);
				this.data.RemoveAt(index+1);
			}
		}
	}

}

// The individual data spots that can either be dead data or free unused data
public struct DataHole{
	public long position;
	public bool infinite;
	public int size;

	public DataHole(long pos, int size, bool infinite=false){
		this.position = pos;
		this.infinite = infinite;
		this.size = size;
	}

	// Turns DataHole into byte format for HLE files
	public void Bytefy(byte[] b, int offset){
		b[offset] = (byte)(this.position >> 56);
		b[offset+1] = (byte)(this.position >> 48);
		b[offset+2] = (byte)(this.position >> 40);
		b[offset+3] = (byte)(this.position >> 32);
		b[offset+4] = (byte)(this.position >> 24);
		b[offset+5] = (byte)(this.position >> 16);
		b[offset+6] = (byte)(this.position >> 8);
		b[offset+7] = (byte)this.position;
		b[offset+8] = (byte)(this.size >> 24);
		b[offset+9] = (byte)(this.size >> 16);
		b[offset+10] = (byte)(this.size >> 8);
		b[offset+11] = (byte)this.size;
	}
}