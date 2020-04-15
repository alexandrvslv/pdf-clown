/*
  Copyright 2008-2012 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfClown.Bytes;
using PdfClown.Documents;
using PdfClown.Documents.Interaction.Navigation;
using PdfClown.Objects;

using System;

namespace PdfClown.Documents.Interaction.Actions
{
    /**
      <summary>'Control drawing during a sequence of actions' action [PDF:1.6:8.5.3].</summary>
    */
    [PDF(VersionEnum.PDF15)]
    public sealed class DoTransition : Action
    {
        #region dynamic
        #region constructors
        /**
          <summary>Creates a new action within the given document context.</summary>
        */
        public DoTransition(
          Document context,
          Transition transition
          ) : base(context, PdfName.Trans)
        { Transition = transition; }

        internal DoTransition(
          PdfDirectObject baseObject
          ) : base(baseObject)
        { }
        #endregion

        #region interface
        #region public
        /**
          <summary>Gets/Sets the transition effect to be used for the update of the display.</summary>
        */
        public Transition Transition
        {
            get => Wrap<Transition>(BaseDataObject[PdfName.Trans]);
            set => BaseDataObject[PdfName.Trans] = value.BaseObject;
        }
        #endregion
        #endregion
        #endregion
    }
}