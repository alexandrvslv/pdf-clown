/*
  Copyright 2011-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Documents.Interchange.Access;
using PdfClown.Objects;
using PdfClown.Tools;
using PdfClown.Util;
using PdfClown.Util.Collections;
using PdfClown.Util.Math;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PdfClown.Documents.Contents.Layers
{
    /// <summary>Optional content group [PDF:1.7:4.10.1].</summary>
    [PDF(VersionEnum.PDF15)]
    public sealed class Layer : LayerEntity, IUILayerNode
    {
        public enum PageElementTypeEnum
        {
            HeaderFooter,
            Foreground,
            Background,
            Logo
        }

        public enum UserTypeEnum
        {
            Individual,
            Title,
            Organization
        }

        /// <summary>Sublayers location within a configuration structure.</summary>
        private class LayersLocation
        {
            /// <summary>Sublayers ordinal position within the parent sublayers.</summary>
            public int Index;
            /// <summary>Parent layer object.</summary>
            public PdfDirectObject ParentLayerObject;
            /// <summary>Parent sublayers object.</summary>
            public PdfArray ParentLayersObject;
            /// <summary>Upper levels.</summary>
            public Stack<LayerLevel> Levels;

            public LayersLocation(PdfDirectObject parentLayerObject, PdfArray parentLayersObject, int index, Stack<LayerLevel> levels)
            {
                ParentLayerObject = parentLayerObject;
                ParentLayersObject = parentLayersObject;
                Index = index;
                Levels = levels;
            }
        }

        /// <summary>Layer state.</summary>
        internal enum StateEnum
        {
            /// <summary>Active.</summary>
            On,
            /// <summary>Inactive.</summary>
            Off
        }

        public static readonly PdfName TypeName = PdfName.OCG;

        private static readonly PdfName MembershipName = PdfName.Get("D-OCMD");
        private ISet<PdfName> intents;
        private List<string> users;

        public Layer(PdfDocument context, string title) : base(context, PdfName.OCG)
        {
            Title = title;

            // Add this layer to the global collection!
            // NOTE: Every layer MUST be included in the global collection [PDF:1.7:4.10.3].
            context.Layer.Layers.BaseDataObject.Add(BaseObject);
        }

        internal Layer(PdfDirectObject baseObject) : base(baseObject)
        { }

        /// <summary>Gets/Sets the type of content controlled by this layer.</summary>
        public string ContentType
        {
            get => GetUsageEntry(PdfName.CreatorInfo).GetString(PdfName.Subtype);
            set => GetUsageEntry(PdfName.CreatorInfo).SetName(PdfName.Subtype, value);
        }

        /// <summary>Gets/Sets the name of the application that created this layer.</summary>
        public string Creator
        {
            get => GetUsageEntry(PdfName.CreatorInfo).GetString(PdfName.Creator);
            set => GetUsageEntry(PdfName.CreatorInfo).SetText(PdfName.Creator, value);
        }

        /// <summary>Gets the dictionary used by the creating application to store application-specific
        /// data associated to this layer.</summary>
        public PdfDictionary CreatorInfo => GetUsageEntry(PdfName.CreatorInfo);

        /// <summary>Deletes this layer, removing also its references from the document (contents included).
        /// </summary>
        public override bool Delete()
        {
            return Delete(false);
        }

        /// <summary>Deletes this layer, removing also its references from the document.</summary>
        /// <param name="preserveContent">Whether its contents are to be excluded from the removal.</param>
        public bool Delete(bool preserveContent)
        {
            if (Document.Layer.Layers.Contains(this))
            {
                var layerManager = new LayerManager();
                layerManager.Remove(preserveContent, this);
            }
            return base.Delete();
        }

        /// <summary>Gets/Sets whether this layer is visible when the document is saved by a viewer
        /// application to a format that does not support layers.</summary>
        public bool? Exportable
        {
            get
            {
                var exportableObject = GetUsageEntry(PdfName.Export).GetString(PdfName.ExportState);
                return exportableObject != null ? StateEnumExtension.Get(exportableObject).IsEnabled() : (bool?)null;
            }
            set
            {
                GetUsageEntry(PdfName.Export)[PdfName.ExportState] = value.HasValue ? StateEnumExtension.Get(value.Value).GetName() : null;
                DefaultConfiguration.SetUsageApplication(PdfName.Export, PdfName.Export, this, value.HasValue);
            }
        }

        /// <summary>Gets/Sets the intended uses of this layer.</summary>
        /// <remarks>For example, many document design applications, such as CAD packages, offer layering
        /// features for collecting groups of graphics together and selectively hiding or viewing them for
        /// the convenience of the author. However, this layering may be different than would be useful to
        /// consumers of the document; therefore, it is possible to specify different intents for layers
        /// within a single document: a given application may decide to use only layers that are of a
        /// specific intent.</remarks>
        /// <returns>Intent collection (it comprises <see cref="IntentEnum"/> names but, for compatibility
        /// with future versions, unrecognized names are allowed). To apply any subsequent change, it has
        /// to be assigned back.</returns>
        /// <seealso cref="IntentEnum"/>
        public ISet<PdfName> Intents
        {
            get => intents ??= GetIntents();
            set
            {
                PdfDirectObject intentObject = null;
                if (value != null
                  && value.Count > 0)
                {
                    if (value.Count == 1) // Single intent.
                    {
                        intentObject = value.First();
                        if (intentObject.Equals(IntentEnum.View.Name())) // Default.
                        { intentObject = null; }
                    }
                    else // Multiple intents.
                    {
                        var intentArray = new PdfArray();
                        foreach (PdfName valueItem in value)
                        { intentArray.Add(valueItem); }
                    }
                }
                BaseDataObject[PdfName.Intent] = intentObject;
            }
        }

        private ISet<PdfName> GetIntents()
        {
            ISet<PdfName> intents = new HashSet<PdfName>();
            PdfDataObject intentObject = BaseDataObject.Resolve(PdfName.Intent);
            if (intentObject != null)
            {
                if (intentObject is PdfArray pdfArray) // Multiple intents.
                {
                    intents.AddRange(pdfArray.OfType<PdfName>());
                }
                else // Single intent.
                {
                    intents.Add((PdfName)intentObject);
                }
            }
            else
            { intents.Add(IntentEnum.View.Name()); }

            return intents;
        }

        /// <summary>Gets/Sets the language of the content controlled by this layer.</summary>
        /// <remarks>The layer whose language matches the current system language is visible.</remarks>
        public LanguageIdentifier Language
        {
            get => LanguageIdentifier.Wrap(GetUsageEntry(PdfName.Language)[PdfName.Lang]);
            set
            {
                GetUsageEntry(PdfName.Language)[PdfName.Lang] = PdfObjectWrapper.GetBaseObject(value);
                DefaultConfiguration.SetUsageApplication(PdfName.View, PdfName.Language, this, value != null);
                DefaultConfiguration.SetUsageApplication(PdfName.Print, PdfName.Language, this, value != null);
                DefaultConfiguration.SetUsageApplication(PdfName.Export, PdfName.Language, this, value != null);
            }
        }

        /// <summary>Gets/Sets whether a partial match (that is, the language matches but not the locale)
        /// with the current system language is enough to keep this layer visible.</summary>
        public bool LanguagePreferred
        {
            get => PdfName.ON.Equals(GetUsageEntry(PdfName.Language).Get<PdfName>(PdfName.Preferred));
            set => GetUsageEntry(PdfName.Language)[PdfName.Preferred] = value ? PdfName.ON : null;
        }

        /// <summary>Gets/Sets whether the default visibility of this layer cannot be changed through the
        /// user interface of a viewer application.</summary>
        public bool Locked
        {
            get => DefaultConfiguration.BaseDataObject.Resolve<PdfArray>(PdfName.Locked).Contains(BaseObject);
            set
            {
                PdfArray lockedArrayObject = DefaultConfiguration.BaseDataObject.Resolve<PdfArray>(PdfName.Locked);
                if (!lockedArrayObject.Contains(BaseObject))
                { lockedArrayObject.Add(BaseObject); }
            }
        }

        public override LayerEntity Membership
        {
            get
            {
                LayerEntity membership = Wrap<LayerMembership>(BaseDataObject[MembershipName]);
                if (membership == null)
                {
                    var location = FindLayersLocation();
                    if (location == null || location.ParentLayerObject == null)
                    { membership = this; }
                    else
                    {
                        BaseDataObject[MembershipName] = (membership = new LayerMembership(Document)).BaseObject;
                        membership.VisibilityPolicy = VisibilityPolicyEnum.AllOn; // NOTE: Forces visibility to depend on all the ascendant layers.
                        membership.VisibilityMembers.Add(this);
                        membership.VisibilityMembers.Add(new Layer(location.ParentLayerObject));
                        foreach (var level in location.Levels)
                        {
                            var layerObject = level.LayerObject;
                            if (layerObject != null)
                            { membership.VisibilityMembers.Add(new Layer(layerObject)); }
                        }
                    }
                }
                return membership;
            }
        }

        /// <summary>Gets/Sets the type of pagination artifact this layer contains.</summary>
        public PageElementTypeEnum? PageElementType
        {
            get => PageElementTypeEnumExtension.Get(GetUsageEntry(PdfName.PageElement).GetString(PdfName.Subtype));
            set => GetUsageEntry(PdfName.PageElement)[PdfName.Subtype] = value.HasValue ? value.Value.GetName() : null;
        }

        /// <summary>Gets the parent layer.</summary>
        public Layer Parent
        {
            get
            {
                var location = FindLayersLocation();
                return location != null ? Wrap<Layer>(location.ParentLayerObject) : null;
            }
        }

        /// <summary>Gets/Sets whether this layer is visible when the document is printed from a viewer
        /// application.</summary>
        public bool? Printable
        {
            get
            {
                var printableObject = GetUsageEntry(PdfName.Print)?.GetString(PdfName.PrintState);
                return printableObject != null ? StateEnumExtension.Get(printableObject).IsEnabled() : (bool?)null;
            }
            set
            {
                GetUsageEntry(PdfName.Print)[PdfName.PrintState] = value.HasValue ? StateEnumExtension.Get(value.Value).GetName() : null;
                DefaultConfiguration.SetUsageApplication(PdfName.Print, PdfName.Print, this, value.HasValue);
            }
        }

        /// <summary>Gets/Sets the type of printable content controlled by this layer.</summary>
        public string PrintType
        {
            get => GetUsageEntry(PdfName.Print).GetString(PdfName.Subtype);
            set => GetUsageEntry(PdfName.Print).SetName(PdfName.Subtype, value);
        }

        public override string ToString()
        { return "Layer {\"" + Title + "\" " + BaseObject + "}"; }

        /// <summary>Gets/Sets the names of the users for whom this layer is primarily intended.</summary>
        public IList<string> Users
        {
            get => users ??= GetUsers();
            set
            {
                users = null;
                PdfDirectObject usersObject = null;
                if (value != null && value.Count > 0)
                {
                    if (value.Count == 1)
                    { usersObject = new PdfTextString(value[0]); }
                    else
                    {
                        var usersArray = new PdfArray();
                        foreach (var user in value)
                        { usersArray.Add(new PdfTextString(user)); }
                        usersObject = usersArray;
                    }
                }
                GetUsageEntry(PdfName.User)[PdfName.Name] = usersObject;
                DefaultConfiguration.SetUsageApplication(PdfName.View, PdfName.User, this, usersObject != null);
                DefaultConfiguration.SetUsageApplication(PdfName.Print, PdfName.User, this, usersObject != null);
                DefaultConfiguration.SetUsageApplication(PdfName.Export, PdfName.User, this, usersObject != null);
            }
        }

        private List<string> GetUsers()
        {
            var users = new List<string>();
            var usersObject = GetUsageEntry(PdfName.User).Resolve(PdfName.Name);
            if (usersObject is IPdfString pdfString)
            { users.Add(pdfString.StringValue); }
            else if (usersObject is PdfArray pdfArray)
            {
                foreach (var userObject in pdfArray.OfType<IPdfString>())
                { users.Add(userObject.StringValue); }
            }

            return users;
        }

        /// <summary>Gets/Sets the type of the users for whom this layer is primarily intended.</summary>
        public UserTypeEnum? UserType
        {
            get => UserTypeEnumExtension.Get(GetUsageEntry(PdfName.User).GetString(PdfName.Type));
            set => GetUsageEntry(PdfName.User)[PdfName.Type] = value.HasValue ? value.Value.GetName() : null;
        }

        /// <summary>Gets/Sets whether this layer is visible when the document is opened in a viewer
        /// application.</summary>
        public bool? Viewable
        {
            get
            {
                var viewableObject = GetUsageEntry(PdfName.View).GetString(PdfName.ViewState);
                return viewableObject != null ? StateEnumExtension.Get(viewableObject).IsEnabled() : (bool?)null;
            }
            set
            {
                GetUsageEntry(PdfName.View)[PdfName.ViewState] = value.HasValue ? StateEnumExtension.Get(value.Value).GetName() : null;
                DefaultConfiguration.SetUsageApplication(PdfName.View, PdfName.View, this, value.HasValue);
            }
        }

        /// <remarks>Default membership's <see cref="LayerMembership.VisibilityExpression">
        /// VisibilityExpression</see> is undefined as <see cref="VisibilityMembers"/> is used instead.
        /// </remarks>
        public override VisibilityExpression VisibilityExpression
        {
            get => null;
            set => throw new NotSupportedException();
        }

        /// <remarks>Default membership's <see cref="LayerMembership.VisibilityMembers">VisibilityMembers
        /// </see> collection is immutable as it's expected to represent the hierarchical line of this
        /// layer.</remarks>
        public override IList<Layer> VisibilityMembers
        {
            get
            {
                var membership = Membership;
                return membership == this ? new List<Layer> { this }.AsReadOnly() : new ReadOnlyCollection<Layer>(membership.VisibilityMembers);
            }
            set => throw new NotSupportedException();
        }

        public override VisibilityPolicyEnum VisibilityPolicy
        {
            get
            {
                var membership = Membership;
                return membership == this ? VisibilityPolicyEnum.AllOn : membership.VisibilityPolicy;
            }
            set
            {
                var membership = Membership;
                if (membership != this)
                { membership.VisibilityPolicy = value; }
            }
        }

        /// <summary>Gets/Sets whether this layer is initially visible in any kind of application.</summary>
        public bool Visible
        {
            get => DefaultConfiguration.IsVisible(this);
            set => DefaultConfiguration.SetVisible(this, value);
        }

        /// <summary>Gets/Sets the range of magnifications at which the content in this layer is best
        /// viewed in a viewer application.</summary>
        /// <returns>Zoom interval (minimum included, maximum excluded); valid values range from 0 to
        /// <code>double.PositiveInfinity</code>, where 1 corresponds to 100% magnification.</returns>
        public Interval<double> ZoomRange
        {
            get
            {
                PdfDictionary zoomDictionary = GetUsageEntry(PdfName.Zoom);
                var minObject = zoomDictionary.GetDouble(PdfName.min);
                var maxObject = zoomDictionary.GetDouble(PdfName.max, double.PositiveInfinity);
                return new Interval<double>(minObject, maxObject);
            }
            set
            {
                if (value != null)
                {
                    PdfDictionary zoomDictionary = GetUsageEntry(PdfName.Zoom);
                    zoomDictionary.Set(PdfName.min, value.Low != 0 ? value.Low : null);
                    zoomDictionary.Set(PdfName.max, value.High != double.PositiveInfinity ? value.High : null);
                }
                else
                { Usage.Remove(PdfName.Zoom); }
                DefaultConfiguration.SetUsageApplication(PdfName.View, PdfName.Zoom, this, value != null);
            }
        }

        public UILayers Children
        {
            get => FindLayersLocation() is LayersLocation location ? Wrap<UILayers>(location.ParentLayersObject.GetOrCreate<PdfArray>(location.Index)) : null;
        }

        public string Title
        {
            get => BaseDataObject.GetString(PdfName.Name);
            set => BaseDataObject.SetText(PdfName.Name, value);
        }

        private LayerConfiguration DefaultConfiguration => Document.Layer.DefaultConfiguration;

        /// <summary>Finds the location of the sublayers object in the default configuration; in case no
        /// sublayers object is associated to this object, its virtual position is indicated.</summary>
        private LayersLocation FindLayersLocation() => FindLayersLocation(DefaultConfiguration);

        /// <summary>Finds the location of the sublayers object in the specified configuration; in case no
        /// sublayers object is associated to this object, its virtual position is indicated.</summary>
        /// <param name="configuration">Configuration context.</param>
        /// <returns><code>null</code>, if this layer is outside the specified configuration.</returns>
        private LayersLocation FindLayersLocation(LayerConfiguration configuration)
        {
            /*
              NOTE: As layers are only weakly tied to configurations, their sublayers have to be sought
              through the configuration structure tree.
            */
            PdfDirectObject levelLayerObject = null;
            PdfDirectObject currentLayerObject = null;
            var levelObject = configuration.UILayers.BaseDataObject;
            var levelIterator = (IEnumerator<PdfDirectObject>)levelObject.GetEnumerator();
            var levelIterators = new Stack<LayerLevel>();
            var thisObject = BaseObject;
            while (true)
            {
                if (!levelIterator.MoveNext())
                {
                    if (levelIterators.Count == 0)
                        break;

                    var levelItems = levelIterators.Pop();
                    levelObject = levelItems.Object;
                    levelIterator = levelItems.Iterator;
                    levelLayerObject = levelItems.LayerObject;
                    currentLayerObject = null;
                }
                else
                {
                    PdfDirectObject nodeObject = levelIterator.Current;
                    PdfDataObject nodeDataObject = PdfObject.Resolve(nodeObject);
                    if (nodeDataObject is PdfDictionary)
                    {
                        if (nodeObject.Equals(thisObject))
                            /*
                              NOTE: Sublayers are expressed as an array immediately following the parent layer node.
                            */
                            return new LayersLocation(levelLayerObject, levelObject, levelObject.IndexOf(thisObject) + 1, levelIterators);

                        currentLayerObject = nodeObject;
                    }
                    else if (nodeDataObject is PdfArray)
                    {
                        levelIterators.Push(new LayerLevel(levelObject, levelIterator, levelLayerObject));
                        levelObject = (PdfArray)nodeDataObject;
                        levelIterator = levelObject.GetEnumerator();
                        levelLayerObject = currentLayerObject;
                        currentLayerObject = null;
                    }
                }
            }
            return null;
        }

        private class LayerLevel
        {
            public LayerLevel(PdfArray levelObject, IEnumerator<PdfDirectObject> levelIterator, PdfDirectObject levelLayerObject)
            {
                Object = levelObject;
                Iterator = levelIterator;
                LayerObject = levelLayerObject;
            }

            public PdfArray Object { get; }
            public IEnumerator<PdfDirectObject> Iterator { get; }
            public PdfDirectObject LayerObject { get; }
        }

        private PdfDictionary GetUsageEntry(PdfName key) => Usage.Resolve<PdfDictionary>(key);

        private PdfDictionary Usage => BaseDataObject.Resolve<PdfDictionary>(PdfName.Usage);
    }

    internal static class PageElementTypeEnumExtension
    {
        private static readonly BiDictionary<Layer.PageElementTypeEnum, string> codes;

        static PageElementTypeEnumExtension()
        {
            codes = new BiDictionary<Layer.PageElementTypeEnum, string>
            {
                [Layer.PageElementTypeEnum.Background] = PdfName.BG.StringValue,
                [Layer.PageElementTypeEnum.Foreground] = PdfName.FG.StringValue,
                [Layer.PageElementTypeEnum.HeaderFooter] = PdfName.HF.StringValue,
                [Layer.PageElementTypeEnum.Logo] = PdfName.L.StringValue
            };
        }

        public static Layer.PageElementTypeEnum? Get(string name)
        {
            if (name == null)
                return null;

            Layer.PageElementTypeEnum? pageElementType = codes.GetKey(name);
            if (!pageElementType.HasValue)
                throw new NotSupportedException("Page element type unknown: " + name);

            return pageElementType;
        }

        public static PdfName GetName(this Layer.PageElementTypeEnum pageElementType) => PdfName.Get(codes[pageElementType], true);
    }

    internal static class StateEnumExtension
    {
        private static readonly BiDictionary<Layer.StateEnum, string> codes;

        static StateEnumExtension()
        {
            codes = new BiDictionary<Layer.StateEnum, string>
            {
                [Layer.StateEnum.On] = PdfName.ON.StringValue,
                [Layer.StateEnum.Off] = PdfName.OFF.StringValue
            };
        }

        public static Layer.StateEnum Get(string name)
        {
            if (name == null)
                return Layer.StateEnum.On;

            Layer.StateEnum? state = codes.GetKey(name);
            if (!state.HasValue)
                throw new NotSupportedException("State unknown: " + name);

            return state.Value;
        }

        public static Layer.StateEnum Get(bool? enabled) => !enabled.HasValue || enabled.Value ? Layer.StateEnum.On : Layer.StateEnum.Off;

        public static PdfName GetName(this Layer.StateEnum state) => PdfName.Get(codes[state], true);

        public static bool IsEnabled(this Layer.StateEnum state) => state == Layer.StateEnum.On;
    }

    internal static class UserTypeEnumExtension
    {
        private static readonly BiDictionary<Layer.UserTypeEnum, string> codes;

        static UserTypeEnumExtension()
        {
            codes = new BiDictionary<Layer.UserTypeEnum, string>
            {
                [Layer.UserTypeEnum.Individual] = PdfName.Ind.StringValue,
                [Layer.UserTypeEnum.Organization] = PdfName.Org.StringValue,
                [Layer.UserTypeEnum.Title] = PdfName.Ttl.StringValue
            };
        }

        public static Layer.UserTypeEnum? Get(string name)
        {
            if (name == null)
                return null;

            Layer.UserTypeEnum? userType = codes.GetKey(name);
            if (!userType.HasValue)
                throw new NotSupportedException("User type unknown: " + name);

            return userType;
        }

        public static PdfName GetName(this Layer.UserTypeEnum userType) => PdfName.Get(codes[userType], true);
    }
}