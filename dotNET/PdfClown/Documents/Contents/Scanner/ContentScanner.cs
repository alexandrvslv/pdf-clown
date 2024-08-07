/*
  Copyright 2007-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Clown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the License, or (at your option) any later version.

  This Program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY,
  either expressed or implied; without even the implied warranty of MERCHANTABILITY or
  FITNESS FOR A PARTICULAR PURPOSE. See the License for more details.

  You should have received a copy of the GNU Lesser General Public License along with this
  Program (see README files); if not, go to the GNU website (http://www.gnu.org/licenses/).

  Redistribution and use, with or without modification, are permitted provided that such
  redistributions retain the above copyright notice, license and disclaimer, along with
  this list of conditions.
*/

using PdfClown.Documents.Contents.Objects;
using PdfClown.Documents.Contents.Scanner;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfClown.Documents.Contents
{
    /// <summary>Content objects scanner.</summary>
    /// <remarks>
    ///   <para>It wraps the <see cref="Contents">content objects collection</see> to scan its graphics state
    ///   through a forward cursor.</para>
    ///   <para>Scanning is performed at an arbitrary deepness, according to the content objects nesting:
    ///   each depth level corresponds to a scan level so that at any time it's possible to seamlessly
    ///   navigate across the levels (see <see cref="ParentLevel"/>, <see cref="ChildLevel"/>).</para>
    /// </remarks>
    public sealed partial class ContentScanner
    {
        /// <summary>Handles the scan start notification.</summary>
        /// <param name="scanner">Content scanner started.</param>
        public delegate void OnStartEventHandler(ContentScanner scanner);

        ///<summary>Notifies the scan start.</summary>
        public event OnStartEventHandler OnStart;

        private static readonly int StartIndex = -1;

        ///Child level.
        private ContentScanner childLevel;
        ///Content objects collection.
        private ContentWrapper contents;
        ///Current object index at this level.
        private int index = 0;
        ///Object collection at this level.
        private IList<ContentObject> objects;

        ///Parent level.
        private ContentScanner parentLevel;
        private ContentScanner resourceParentLevel;
        ///Current graphics state.
        private GraphicsState state;

        ///Rendering context.
        private SKCanvas canvas;
        ///Rendering object.
        private SKPath path;

        /// <summary>Device-independent size of the graphics canvas.</summary>
        private SKRect contextBox;

        /// <summary>Instantiates a top-level content scanner.</summary>
        /// <param name="contents">Content objects collection to scan.</param>
        public ContentScanner(ContentWrapper contents)
            : this(contents, contents, null, null, contents.ContentContext.Box)
        { }

        /// <summary>Instantiates a top-level content scanner.</summary>
        /// <param name="context">Content context containing the content objects collection to scan.</param>
        public ContentScanner(IContentContext context) : this(context.Contents)
        { }

        /// <summary>Instantiates a child-level content scanner for <see cref="PdfClown.Documents.Contents.XObjects.FormXObject">external form</see>.</summary>
        /// <param name="context">External form.</param>
        /// <param name="parentLevel">Parent scan level.</param>
        public ContentScanner(IContentContext context, ContentScanner parentLevel)
            : this(context.Contents, parentLevel, parentLevel.Canvas, parentLevel.ContextBox)
        { }

        public ContentScanner(IContentContext context, SKCanvas canvas, SKRect box, SKColor? clearColor = null)
            : this(context.Contents, null, canvas, box, clearColor)
        { }

        /// <summary>Instantiates a child-level content scanner.</summary>
        /// <param name="parentLevel">Parent scan level.</param>
        private ContentScanner(ContentScanner parentLevel)
            : this(parentLevel.contents, ((CompositeObject)parentLevel.Current).Objects, parentLevel, parentLevel.Canvas, parentLevel.ContextBox)
        { }

        private ContentScanner(ContentWrapper contents, ContentScanner parentLevel, SKCanvas canvas, SKRect box, SKColor? clearColor = null)
            : this(contents, contents, parentLevel, canvas, box, clearColor)
        { }

        private ContentScanner(ContentWrapper contentWrapper, IList<ContentObject> objects, ContentScanner parentLevel, SKCanvas canvas, SKRect box, SKColor? clearColor = null)
        {
            this.parentLevel = parentLevel;
            this.contents = contentWrapper;
            this.objects = objects;
            this.canvas = canvas;
            contextBox = box;
            ClearColor = clearColor;
            ClearCanvas();
            MoveStart();
        }

        /// <summary>Size of the graphics canvas.</summary>
        /// <remarks>According to the current processing (whether it is device-independent scanning or
        /// device-based rendering), it may be expressed, respectively, in user-space units or in
        /// device-space units.</remarks>
        public SKRect CanvasBox => Canvas?.DeviceClipBounds ?? ContextBox;

        /// <summary>Gets the current child scan level.</summary>
        public ContentScanner ChildLevel => childLevel;

        /// <summary>Gets the content context associated to the content objects collection.</summary>
        public IContentContext Context => contents.ContentContext;

        /// <summary>Gets the content objects collection this scanner is inspecting.</summary>
        public ContentWrapper Contents => contents;

        /// <summary>Gets the size of the current imageable area in user-space units.</summary>
        public SKRect ContextBox => contextBox;

        /// <summary>Gets/Sets the current content object.</summary>
        public ContentObject Current
        {
            get
            {
                if (index < 0 || index >= objects.Count)
                    return null;

                return objects[index];
            }
            set
            {
                objects[index] = value;
                Refresh();
            }
        }

        /// <summary>Gets the current content object's information.</summary>
        public GraphicsObjectWrapper CurrentWrapper => GraphicsObjectWrapper.Get(this);

        /// <summary>Gets the current position.</summary>
        public int Index => index;

        /// <summary>Gets the current parent object.</summary>
        public CompositeObject Parent => ParentLevel?.Current as CompositeObject;

        /// <summary>Gets the parent scan level.</summary>
        public ContentScanner ParentLevel => parentLevel;

        public ContentScanner ResourceParent
        {
            get => resourceParentLevel ?? ParentLevel;
            internal set => resourceParentLevel = value;
        }

        /// <summary>Inserts a content object at the current position.</summary>
        public void Insert(ContentObject obj)
        {
            if (index == -1)
            { index = 0; }

            objects.Insert(index, obj);
            Refresh();
        }

        /// <summary>Inserts content objects at the current position.</summary>
        /// <remarks>After the insertion is complete, the lastly-inserted content object is at the current position.</remarks>
        public void Insert<T>(ICollection<T> objects) where T : ContentObject
        {
            int index = 0;
            int count = objects.Count;
            foreach (ContentObject obj in objects)
            {
                Insert(obj);

                if (++index < count)
                { MoveNext(); }
            }
        }

        ///<summary>Gets whether this level is the root of the hierarchy.</summary>
        public bool IsRootLevel => ParentLevel == null;

        /// <summary>Moves to the object at the given position.</summary>
        /// <param name="index">New position.</param>
        /// <returns>Whether the object was successfully reached.</returns>
        public bool Move(int index)
        {
            if (this.index > index)
            { MoveStart(); }

            while (this.index < index
              && MoveNext()) ;

            return Current != null;
        }

        /// <summary>Moves after the last object.</summary>
        public void MoveEnd()
        {
            MoveLast();
            MoveNext();
        }

        /// <summary>Moves to the first object.</summary>
        /// <returns>Whether the first object was successfully reached.</returns>
        public bool MoveFirst()
        {
            MoveStart();
            return MoveNext();
        }

        /// <summary>Moves to the last object.</summary>
        /// <returns>Whether the last object was successfully reached.</returns>
        public bool MoveLast()
        {
            int lastIndex = objects.Count - 1;
            while (index < lastIndex)
            { MoveNext(); }

            return Current != null;
        }

        /// <summary>Moves to the next object.</summary>
        /// <returns>Whether the next object was successfully reached.</returns>
        public bool MoveNext()
        {
            // Scanning the current graphics state...
            Current?.Scan(State);

            // Moving to the next object...
            if (index < objects.Count)
            { index++; Refresh(); }

            return Current != null;
        }

        /// <summary>Moves before the first object.</summary>
        public void MoveStart()
        {
            index = StartIndex;
            if (state == null)
            {
                state = ParentLevel?.state.Clone(this) ?? new GraphicsState(this);
            }
            else
            {
                if (ParentLevel is ContentScanner parentScanner)
                {
                    ParentLevel.state.CopyTo(state);
                }
                else
                {
                    state.Initialize();
                }
            }

            NotifyStart();
            Refresh();
        }

        private void ClearCanvas()
        {
            if (Canvas == null
                || ParentLevel != null)
                return;
            //var mapped = state.Ctm.MapRect(ContextBox);
            Canvas.ClipRect(ContextBox);
            if (ClearColor is SKColor color)
            {
                Canvas.Clear(color);
            }
        }

        /// <summary>Removes the content object at the current position.</summary>
        /// <returns>Removed object.</returns>
        public ContentObject Remove()
        {
            ContentObject removedObject = Current;
            objects.RemoveAt(index);
            Refresh();

            return removedObject;
        }

        /// <summary>Renders the contents into the specified context.</summary>
        public void Render()
        {
            Render(null);
        }

        /// <summary>Renders the contents into the specified object.</summary>
        /// <param name="path">Rendering object.</param>
        public void Render(SKPath path)
        {
            this.path = path;

            // Scan this level for rendering!
            MoveStart();
            while (MoveNext()) ;
        }

        /// <summary>Gets the rendering context.</summary>
        /// <returns><code>null</code> in case of dry scanning.</returns>
        public SKCanvas Canvas
        {
            get => canvas;
            internal set => canvas = value;
        }

        /// <summary>Gets the rendering object.</summary>
        /// <returns><code>null</code> in case of scanning outside a shape.</returns>
        public SKPath Path
        {
            get => path;
            internal set => path = value;
        }

        ///<summary>Gets the root scan level.</summary>
        public ContentScanner RootLevel
        {
            get
            {
                ContentScanner level = this;
                while (level.ParentLevel != null)
                {
                    level = level.ParentLevel;
                }
                return level;
            }
        }

        /// <summary>Gets the current graphics state applied to the current content object.</summary>
        public GraphicsState State => state;

        public SKColor? ClearColor { get; set; }

#pragma warning disable 0628
        /// <summary>Notifies the scan start to listeners.</summary>
        protected void NotifyStart()
        {
            OnStart?.Invoke(this);
        }
#pragma warning restore 0628

        /// <summary>Synchronizes the scanner state.</summary>
        private void Refresh()
        {
            if (Current is CompositeObject)
            { childLevel = new ContentScanner(this); }
            else
            { childLevel = null; }
        }
    }
}