/*
  Copyright 2006-2012 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Objects;
using PdfClown.Util.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PdfClown.Documents
{
    /// <summary>Document pages collection [PDF:1.6:3.6.2].</summary>
    [PDF(VersionEnum.PDF10)]
    public sealed class Pages : PdfObjectWrapper<PdfDictionary>, IExtList<PdfPage>, IList<PdfPage>
    {
        public struct Enumerator : IEnumerator<PdfPage>
        {
            /// <summary>Collection size.</summary>
            private int count;
            /// <summary>Index of the next item.</summary>
            private int index;

            /// <summary>Current page.</summary>
            private PdfPage current;

            /// <summary>Current level index.</summary>
            private int levelIndex;

            /// <summary>Stacked level indexes.</summary>
            private Stack<int> levelIndexes;

            private HashSet<PdfArray> containers;

            /// <summary>Current child tree nodes.</summary>
            private PdfArray kids;
            /// <summary>Current parent tree node.</summary>
            private PdfDictionary parent;

            internal Enumerator(Pages pages)
            {
                index = 0;
                current = null;
                levelIndex = 0;
                levelIndexes = new Stack<int>();
                containers = new HashSet<PdfArray>();
                count = pages.Count;
                parent = pages.BaseDataObject;
                kids = parent.Get<PdfArray>(PdfName.Kids);
            }

            public PdfPage Current => current;

            object IEnumerator.Current => current;

            public bool MoveNext()
            {
                if (index == count)
                    return false;

                //NOTE: As stated in [PDF:1.6:3.6.2], page retrieval is a matter of diving
                //  inside a B - tree.
                //  This is a special adaptation of the get() algorithm necessary to keep
                //  a low overhead throughout the page tree scan(using the get() method
                //  would have implied a nonlinear computational cost).

                //NOTE: Algorithm:
                //  1. [Vertical, down] We have to go downward the page tree till we reach
                //a page(leaf node).
                //  2. [Horizontal] Then we iterate across the page collection it belongs to,
                //  repeating step 1 whenever we find a subtree.
                //  3. [Vertical, up] When leaf-nodes scan is complete, we go upward solving
                //  parent nodes, repeating step 2.

                while (true)
                {
                    // Did we complete current page-tree-branch level?
                    if (kids.Count == levelIndex) // Page subtree complete.
                    {
                        // 3. Go upward one level.
                        // Restore node index at the current level!
                        levelIndex = levelIndexes.Pop() + 1; // Next node (partially scanned level).
                                                             // Move upward!
                        parent = parent.Get<PdfDictionary>(PdfName.Parent);
                        kids = parent.Get<PdfArray>(PdfName.Kids);
                    }
                    else // Page subtree incomplete.
                    {
                        PdfReference kidReference = (PdfReference)kids[levelIndex];
                        PdfDataObject kidObject = kidReference.DataObject;
                        if (kidObject is PdfDictionary kid)
                        {
                            // Is current kid a page object?
                            if (PdfName.Page.Equals(kid.Get<PdfName>(PdfName.Type))) // Page object.
                            {
                                // 2. Page found.
                                index++; // Absolute page index.
                                levelIndex++; // Current level node index.

                                current = Wrap<PdfPage>(kidReference);
                                return true;
                            }
                            else // Page tree node.
                            {
                                // 1. Go downward one level.
                                // Save node index at the current level!
                                levelIndexes.Push(levelIndex);
                                // Move downward!
                                parent = kid;
                                kids = parent.Get<PdfArray>(PdfName.Kids);
                                if (containers.Contains(kids))
                                    return false;
                                containers.Add(kids);
                                levelIndex = 0; // First node (new level).
                            }
                        }
                        else
                        {
                            return false;
                            //throw new Exception($"TODO Support type {kidObject.GetType()} in page enumeration!");
                        }
                    }
                }
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            { }
        }

        /*
          TODO:IMPL A B-tree algorithm should be implemented to optimize the inner layout
          of the page tree (better insertion/deletion performance). In this case, it would
          be necessary to keep track of the modified tree nodes for incremental update.
        */
        public Pages(PdfDocument context)
            : base(context, new PdfDictionary(3)
            {
                { PdfName.Type, PdfName.Pages },
                { PdfName.Kids, new PdfArray() },
                { PdfName.Count, PdfInteger.Default }
            })
        { }

        public Pages(PdfDirectObject baseObject) : base(baseObject)
        { }

        public IList<PdfPage> GetRange(int index, int count)
        {
            return GetSlice(index, index + count);
        }

        public IList<PdfPage> GetSlice(int fromIndex, int toIndex)
        {
            var pages = new List<PdfPage>(toIndex - fromIndex);
            int i = fromIndex;
            while (i < toIndex)
            { pages.Add(this[i++]); }

            return pages;
        }

        public void InsertAll<TVar>(int index, ICollection<TVar> pages)
          where TVar : PdfPage
        {
            CommonAddAll(index, pages);
        }

        public void AddAll<TVar>(ICollection<TVar> pages)
          where TVar : PdfPage
        {
            CommonAddAll(-1, pages);
        }

        public void RemoveAll<TVar>(ICollection<TVar> pages)
          where TVar : PdfPage
        {
            //NOTE: The interface contract doesn't prescribe any relation among the removing-collection's
            //items, so we cannot adopt the optimized approach of the add* (...) methods family,
            //where adding-collection's items are explicitly ordered.

            foreach (PdfPage page in pages)
            { Remove(page); }
        }

        public int RemoveAll(Predicate<PdfPage> match)
        {
            //NOTE: Removal is indirectly fulfilled through an intermediate collection
            //in order not to interfere with the enumerator execution.

            var removingPages = new List<PdfPage>();
            foreach (PdfPage page in this)
            {
                if (match(page))
                { removingPages.Add(page); }
            }

            RemoveAll(removingPages);

            return removingPages.Count;
        }

        public int IndexOf(PdfPage page) => page.Index;

        public void Insert(int index, PdfPage page)
        {
            CommonAddAll(index, (ICollection<PdfPage>)new PdfPage[] { page });
        }

        public void RemoveAt(int index) => Remove(this[index]);

        public PdfPage this[int index]
        {
            get
            {
                //NOTE: As stated in [PDF:1.6:3.6.2], to retrieve pages is a matter of diving
                //inside a B - tree.To keep it as efficient as possible, this implementation
                //does NOT adopt recursion to deepen its search, opting for an iterative
                //strategy instead.

                int pageOffset = 0;
                PdfDictionary parent = BaseDataObject;
                var kids = parent.Get<PdfArray>(PdfName.Kids);
                for (int i = 0; i < kids.Count; i++)
                {
                    var kidReference = kids.Get<PdfReference>(i);
                    PdfDictionary kid = (PdfDictionary)kidReference.DataObject;
                    // Is current kid a page object?
                    if (PdfName.Page.Equals(kid.Get<PdfName>(PdfName.Type))) // Page object.
                    {
                        // Did we reach the searched position?
                        if (pageOffset == index) // Vertical scan (we finished).
                                                 // We got it!
                            return Wrap<PdfPage>(kidReference);
                        else // Horizontal scan (go past).
                             // Cumulate current page object count!
                            pageOffset++;
                    }
                    else // Page tree node.
                    {
                        // Does the current subtree contain the searched page?
                        var count = kid.GetInt(PdfName.Count);
                        if (count + pageOffset > index) // Vertical scan (deepen the search).
                        {
                            // Go down one level!
                            parent = kid;
                            kids = parent.Get<PdfArray>(PdfName.Kids);
                            i = -1;
                        }
                        else // Horizontal scan (go past).
                        {
                            // Cumulate current subtree count!
                            pageOffset += count;
                        }
                    }
                }

                return null;
            }
            set
            {
                RemoveAt(index);
                Insert(index, value);
            }
        }

        public void Add(PdfPage page) => CommonAddAll(-1, new PdfPage[] { page });

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(PdfPage page) => ((IEnumerable<PdfPage>)this).Contains(page);

        public void CopyTo(PdfPage[] entires, int index)
        {
            foreach (PdfPage entry in this)
            {
                entires[index++] = entry;
            }
        }

        public int Count => BaseDataObject.GetInt(PdfName.Count);

        public bool IsReadOnly => false;

        public bool Remove(PdfPage page)
        {
            PdfDictionary pageData = page.BaseDataObject;
            // Get the parent tree node!
            PdfDirectObject parent = pageData[PdfName.Parent];
            PdfDictionary parentData = (PdfDictionary)parent.Resolve();
            // Get the parent's page collection!
            PdfDirectObject kids = parentData[PdfName.Kids];
            PdfArray kidsData = (PdfArray)kids.Resolve();
            // Remove the page!
            kidsData.Remove(page.BaseObject);

            // Unbind the page from its parent!
            pageData[PdfName.Parent] = null;

            // Decrementing the pages counters...
            do
            {
                // Get the page collection counter!
                var count = parentData.GetInt(PdfName.Count);
                // Decrement the counter at the current level!
                parentData.Set(PdfName.Count, count - 1);

                // Iterate upward!
                parent = parentData[PdfName.Parent];
                parentData = (PdfDictionary)PdfObject.Resolve(parent);
            } while (parent != null);

            return true;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<PdfPage> IEnumerable<PdfPage>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// Add a collection of pages at the specified position.
        /// <param name="index">Addition position. To append, use value -1.</param>
        /// <param name="pages">Collection of pages to add.</param>
        private void CommonAddAll<TPage>(int index, ICollection<TPage> pages) where TPage : PdfPage
        {
            PdfDirectObject parent;
            PdfDictionary parentData;
            PdfDirectObject kids;
            PdfArray kidsData;
            int offset;
            // Append operation?
            if (index == -1) // Append operation.
            {
                // Get the parent tree node!
                parent = BaseObject;
                parentData = BaseDataObject;
                // Get the parent's page collection!
                kids = parentData[PdfName.Kids];
                kidsData = (PdfArray)PdfObject.Resolve(kids);
                offset = 0; // Not used.
            }
            else // Insert operation.
            {
                // Get the page currently at the specified position!
                var pivotPage = this[index];
                // Get the parent tree node!
                parent = pivotPage.BaseDataObject[PdfName.Parent];
                parentData = (PdfDictionary)parent.Resolve();
                // Get the parent's page collection!
                kids = parentData[PdfName.Kids];
                kidsData = (PdfArray)kids.Resolve();
                // Get the insertion's relative position within the parent's page collection!
                offset = kidsData.IndexOf(pivotPage.BaseObject);
            }

            // Adding the pages...
            foreach (var page in pages)
            {
                // Append?
                if (index == -1) // Append.
                {
                    // Append the page to the collection!
                    kidsData.Add(page.BaseObject);
                }
                else // Insert.
                {
                    // Insert the page into the collection!
                    kidsData.Insert(offset++, page.BaseObject);
                }
                // Bind the page to the collection!
                page.BaseDataObject[PdfName.Parent] = parent;
            }

            // Incrementing the pages counters...
            do
            {
                // Get the page collection counter!
                var count = parentData.GetInt(PdfName.Count);
                // Increment the counter at the current level!
                parentData.Set(PdfName.Count, count + pages.Count);

                // Iterate upward!
                parent = parentData[PdfName.Parent];
                parentData = (PdfDictionary)PdfObject.Resolve(parent);
            } while (parent != null);
        }
    }
}