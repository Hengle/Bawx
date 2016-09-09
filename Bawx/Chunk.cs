﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bawx
{
    public class Chunk
    {
        public const int DefaultSize = 32;

        // avoiding garbage
        private readonly BlockData[] _tmpBlockData;

        private readonly VoxelBuffer<BlockData> _voxelBuffer;
        public readonly VoxelEffect Effect;
        private readonly Vector3 _position;

        public readonly int SizeX;
        public readonly int SizeY;
        public readonly int SizeZ;

        public readonly int TotalSize;

        private int _currentIndex;

        public int BlockCount => _currentIndex;

        public Chunk(GraphicsDevice gd, VoxelEffect effect, Vector3 position, 
            int sizeX = DefaultSize, int sizeY = DefaultSize, int sizeZ = DefaultSize)
        {
            _voxelBuffer = new VoxelBuffer<BlockData>(gd);
            _position = position;
            Effect = effect;
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            TotalSize = sizeX*sizeY*sizeZ;

            _tmpBlockData = new BlockData[1];
        }

        public void SetBlockData(int index, BlockData data)
        {
            _tmpBlockData[0] = data;
            _voxelBuffer.SetData(_tmpBlockData, 0, index, 1);
        }

        /// <summary>
        /// Add a single block from the given block data to this chunk. Do not use this when building a chunk, use <see cref="BuildChunk"/> instead.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void AddBlock(BlockData data)
        {
            if (BlockCount == TotalSize)
                throw new InvalidOperationException("Chunk is full.");

            var block = new Block(data, _currentIndex);
            SetBlockData(_currentIndex, data);
            _currentIndex++;
            // TODO store the blocks in a more manageable format
        }

        public void BuildChunk(BlockData[] data, bool rebuild = false)
        {
            Effect.World = _position;

            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length > TotalSize)
                throw new InvalidOperationException("Too much data for this chunk.");
            if (BlockCount > 0 && !rebuild)
                throw new InvalidOperationException("Chunk is already built, to override set the rebuild flag.");

            _voxelBuffer.Create(data);
            _currentIndex = data.Length;
            // TODO store the blocks in a more manageable format
        }

        public void Draw()
        {
            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _voxelBuffer.Draw();
            }
        }
    }
}