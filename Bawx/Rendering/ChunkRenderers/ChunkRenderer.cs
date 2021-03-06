﻿using System;
using System.Runtime.InteropServices;
using Bawx.Rendering.Effects;
using Bawx.VertexTypes;
using Bawx.VoxelData;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bawx.Rendering.ChunkRenderers
{
    public abstract class ChunkRenderer : IDisposable
    {
        private int _currentIndex;

        protected readonly GraphicsDevice GraphicsDevice;
        protected Chunk Chunk;

        protected static int BlockDataSize = Marshal.SizeOf(typeof(Block));

        /// <summary>
        /// The number of active blocks.
        /// </summary>
        public int BlockCount => _currentIndex;

        /// <summary>
        /// The effect used for rendering.
        /// </summary>
        public readonly VoxelEffect Effect;

        /// <summary>
        /// The number of blocks that can be added to the buffer without rebuilding.
        /// </summary>
        public abstract int FreeBlocks { get; }

        /// <summary>
        /// True if this renderers buffer is full. This mean you have to rebuild the buffer to add blocks.
        /// </summary>
        public bool BufferFull => FreeBlocks == 0;

        #region Initialization

        protected ChunkRenderer(GraphicsDevice graphicsDevice, Vector4[] palette)
        {
            if (graphicsDevice == null)
                throw new ArgumentNullException(nameof(graphicsDevice));
            if (graphicsDevice.GraphicsProfile != GraphicsProfile.HiDef)
                throw new ArgumentException("GraphicsDevice should have the HiDef profile!");
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));
                
            GraphicsDevice = graphicsDevice;
            Effect = new VoxelEffect(graphicsDevice);
            Effect.Palette = palette;
        }

        /// <summary>
        /// True if <see cref="Initialize"/> has been called on this renderer.
        /// </summary>
        public bool Initialized { get; private set; }

        /// <summary>
        /// Initialize this renderer for the given chunk.
        /// </summary>
        /// <param name="chunk">The chunk that this renderer should render.</param>
        /// <param name="blocks">Array of all blocks that should be rendered.</param>
        /// <param name="active">The number of active blocks. Active blocks must precede inactive blocks in blockData.</param>
        /// <param name="maxBlocks">
        /// The total number of blocks to make room for. Some renderers need to resize a buffer when they get full,
        /// so this parameter can be used to reserve some extra room from the start.
        /// </param>
        public void Initialize(Chunk chunk, Block[] blocks, int active, int? maxBlocks = null)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));
            if (Chunk != null && !ReferenceEquals(chunk, Chunk))
                throw new ArgumentException("Renderer was already intialized for a different chunk.");

            if (Initialized)
                Dispose();

            InitializeInternal(chunk, blocks, active, maxBlocks ?? chunk.BlockCount);
            _currentIndex += chunk.BlockCount;
            Initialized = true;
        }

        protected abstract void InitializeInternal(Chunk chunk, Block[] blocks, int active, int maxBlocks);

        #endregion Initialization

        #region Modification

        /// <summary>
        /// Set the block at the given index.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="index"></param>
        public abstract void SetBlock(Block block, int index);

        /// <summary>
        /// Add a block with the given data and returns the index of the created block.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the buffer is full and the <see cref="rebuildIfNeeded"/> flag is not set.</exception>
        /// <param name="block">Data of the block to add.</param>
        /// <param name="rebuildIfNeeded">
        ///   If true, this renderer will rebuild its buffer if it's full before adding the block. 
        ///   Fulness can be checked with <see cref="BufferFull"/> and <see cref="FreeBlocks"/>.
        /// </param>
        /// <returns>The index of the added block.</returns>
        public int AddBlock(Block block, bool rebuildIfNeeded = false)
        {
            // TODO overwrite inactive blocks if any exist.
            if (FreeBlocks == 0)
            {
                if (rebuildIfNeeded)
                    RebuildInternal(BlockCount + 1);
                else
                    throw new InvalidOperationException("The buffer cannot hold any more blocks, rebuild the buffer");
            }
            var index = _currentIndex;
            SetBlock(block, index);
            _currentIndex++;

            return index;
        }

        /// <summary>
        /// Remove the block at the given index. Makes the block inactive until a block is added to overwrite it. 
        /// To remove all inactive blocks and rebuild the buffer use <see cref="Rebuild"/>
        /// </summary>
        /// <param name="index">The index of the block to remove.</param>
        public abstract void RemoveBlock(int index);

        /// <summary>
        /// Rebuild the buffer to remove any empty blocks
        /// </summary>
        /// <param name="maxBlocks">The number of blocks that the buffer should be able to hold. If left at null maxBlocks will be set to <see cref="BlockCount"/>.</param>
        public void Rebuild(int? maxBlocks)
        {
            RebuildInternal(maxBlocks ?? BlockCount);
        }

        protected abstract void RebuildInternal(int maxBlocks);

        #endregion

        #region Rendering

        protected virtual void PreDraw()
        {
        }

        public void Draw()
        {
            PreDraw();

            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                DrawInternal();
            }
        }

        protected abstract void DrawInternal();

        #endregion

        #region IDisposable

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Ensures this voxel buffer is disposed.
        /// </summary>
        ~ChunkRenderer()
        {
            Dispose(false);
            IsDisposed = true;
        }

        /// <summary>
        /// Disposes of this voxel buffer
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Don't call this method directly! Use <see cref="Dispose"/>.
        /// </summary>
        protected abstract void Dispose(bool disposing);

        #endregion

    }
}