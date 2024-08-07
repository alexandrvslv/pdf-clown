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

using PdfClown.Objects;
using PdfClown.Util;

using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfClown.Documents.Contents.Layers
{
    /// <summary>Optional content configuration [PDF:1.7:4.10.3].</summary>
    [PDF(VersionEnum.PDF15)]
    public sealed class LayerConfiguration : PdfObjectWrapper<PdfDictionary>, ILayerConfiguration
    {
        private ISet<PdfName> intents;

        /// <summary>Base state used to initialize the states of all the layers in a document when this
        /// configuration is applied.</summary>
        internal enum BaseStateEnum
        {
            /// <summary>All the layers are visible.</summary>
            On,
            /// <summary>All the layers are invisible.</summary>
            Off,
            /// <summary>All the layers are left unchanged.</summary>
            Unchanged
        }


        public LayerConfiguration(PdfDocument context) : base(context, new PdfDictionary())
        { }

        public LayerConfiguration(PdfDirectObject baseObject) : base(baseObject)
        { }

        public string Creator
        {
            get => BaseDataObject.GetString(PdfName.Creator);
            set => BaseDataObject.SetText(PdfName.Creator, value);
        }

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
                if (intentObject is PdfArray array) // Multiple intents.
                {
                    foreach (var intentItem in array.OfType<PdfName>())
                    { intents.Add(intentItem); }
                }
                else // Single intent.
                { intents.Add((PdfName)intentObject); }
            }
            else
            { intents.Add(IntentEnum.View.Name()); }
            return intents;
        }

        public Array<OptionGroup> OptionGroups => Wrap<Array<OptionGroup>>(BaseDataObject.GetOrCreate<PdfArray>(PdfName.RBGroups));

        public string Title
        {
            get => BaseDataObject.GetString(PdfName.Name);
            set => BaseDataObject.SetText(PdfName.Name, value);
        }

        public UILayers UILayers => Wrap<UILayers>(BaseDataObject.GetOrCreate<PdfArray>(PdfName.Order));

        public UIModeEnum UIMode
        {
            get => UIModeEnumExtension.Get(BaseDataObject.GetString(PdfName.ListMode));
            set => BaseDataObject[PdfName.ListMode] = value.GetName();
        }

        public bool? Visible
        {
            get => BaseStateEnumExtension.Get(BaseDataObject.GetString(PdfName.BaseState)).IsEnabled();
            set
            {
                // NOTE: Base state can be altered only in case of alternate configuration; default ones MUST
                // be set to default state (that is ON).
                if (!(BaseObject.Parent is PdfDictionary)) // Not the default configuration?
                { BaseDataObject[PdfName.BaseState] = BaseStateEnumExtension.Get(value).GetName(); }
            }
        }

        internal bool IsVisible(Layer layer)
        {
            bool? visible = Visible;
            if (!visible.HasValue || visible.Value)
                return !OffLayersObject.Contains(layer.BaseObject);
            else
                return OnLayersObject.Contains(layer.BaseObject);
        }

        /// <summary>Sets the usage application for the specified factors.</summary>
        /// <param name="event">Situation in which this usage application should be used. May be
        ///   <see cref="PdfName.View">View</see>, <see cref="PdfName.Print">Print</see> or <see
        ///   cref="PdfName.Export">Export</see>.</param>
        /// <param name="category">Layer usage entry to consider when managing the states of the layer.
        /// </param>
        /// <param name="layer">Layer which should have its state automatically managed based on its usage
        ///   information.</param>
        /// <param name="retain">Whether this usage application has to be kept or removed.</param>
        internal void SetUsageApplication(PdfName @event, PdfName category, Layer layer, bool retain)
        {
            bool matched = false;
            var usages = BaseDataObject.Resolve<PdfArray>(PdfName.AS);
            foreach (var usage in usages)
            {
                var usageDictionary = (PdfDictionary)usage;
                if (usageDictionary[PdfName.Event].Equals(@event)
                  && usageDictionary.Get<PdfArray>(PdfName.Category).Contains(category))
                {
                    PdfArray usageLayers = usageDictionary.Resolve<PdfArray>(PdfName.OCGs);
                    if (usageLayers.Contains(layer.BaseObject))
                    {
                        if (!retain)
                        { usageLayers.Remove(layer.BaseObject); }
                    }
                    else
                    {
                        if (retain)
                        { usageLayers.Add(layer.BaseObject); }
                    }
                    matched = true;
                }
            }
            if (!matched && retain)
            {
                var usageDictionary = new PdfDictionary();
                {
                    usageDictionary[PdfName.Event] = @event;
                    usageDictionary.Resolve<PdfArray>(PdfName.Category).Add(category);
                    usageDictionary.Resolve<PdfArray>(PdfName.OCGs).Add(layer.BaseObject);
                }
                usages.Add(usageDictionary);
            }
        }

        internal void SetVisible(Layer layer, bool value)
        {
            PdfDirectObject layerObject = layer.BaseObject;
            PdfArray offLayersObject = OffLayersObject;
            PdfArray onLayersObject = OnLayersObject;
            bool? visible = Visible;
            if (!visible.HasValue)
            {
                if (value && !onLayersObject.Contains(layerObject))
                {
                    onLayersObject.Add(layerObject);
                    offLayersObject.Remove(layerObject);
                }
                else if (!value && !offLayersObject.Contains(layerObject))
                {
                    offLayersObject.Add(layerObject);
                    onLayersObject.Remove(layerObject);
                }
            }
            else if (!visible.Value)
            {
                if (value && !onLayersObject.Contains(layerObject))
                { onLayersObject.Add(layerObject); }
            }
            else
            {
                if (!value && !offLayersObject.Contains(layerObject))
                { offLayersObject.Add(layerObject); }
            }
        }

        /// <summary>Gets the collection of the layer objects whose state is set to OFF.</summary>
        private PdfArray OffLayersObject => BaseDataObject.Resolve<PdfArray>(PdfName.OFF);

        /// <summary>Gets the collection of the layer objects whose state is set to ON.</summary>
        private PdfArray OnLayersObject => BaseDataObject.Resolve<PdfArray>(PdfName.ON);
    }

    internal static class BaseStateEnumExtension
    {
        private static readonly BiDictionary<LayerConfiguration.BaseStateEnum, string> codes;

        static BaseStateEnumExtension()
        {
            codes = new BiDictionary<LayerConfiguration.BaseStateEnum, string>
            {
                [LayerConfiguration.BaseStateEnum.On] = PdfName.ON.StringValue,
                [LayerConfiguration.BaseStateEnum.Off] = PdfName.OFF.StringValue,
                [LayerConfiguration.BaseStateEnum.Unchanged] = PdfName.Unchanged.StringValue
            };
        }

        public static LayerConfiguration.BaseStateEnum Get(string name)
        {
            if (name == null)
                return LayerConfiguration.BaseStateEnum.On;

            LayerConfiguration.BaseStateEnum? baseState = codes.GetKey(name);
            if (!baseState.HasValue)
                throw new NotSupportedException("Base state unknown: " + name);

            return baseState.Value;
        }

        public static LayerConfiguration.BaseStateEnum Get(bool? enabled)
        { return enabled.HasValue ? (enabled.Value ? LayerConfiguration.BaseStateEnum.On : LayerConfiguration.BaseStateEnum.Off) : LayerConfiguration.BaseStateEnum.Unchanged; }

        public static PdfName GetName(this LayerConfiguration.BaseStateEnum baseState) => PdfName.Get(codes[baseState], true);

        public static bool? IsEnabled(this LayerConfiguration.BaseStateEnum baseState)
        {
            switch (baseState)
            {
                case LayerConfiguration.BaseStateEnum.On:
                    return true;
                case LayerConfiguration.BaseStateEnum.Off:
                    return false;
                case LayerConfiguration.BaseStateEnum.Unchanged:
                    return null;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}