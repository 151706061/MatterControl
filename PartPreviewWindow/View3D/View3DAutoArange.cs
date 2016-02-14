﻿/*
Copyright (c) 2014, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using MatterHackers.Localizations;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.MeshVisualizer;
using MatterHackers.PolygonMesh;
using MatterHackers.VectorMath;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public partial class View3DWidget
	{
		private async void AutoArrangePartsInBackground()
		{
			if (MeshGroups.Count > 0)
			{
				string progressArrangeParts = LocalizedString.Get("Arranging Parts");
				string progressArrangePartsFull = string.Format("{0}:", progressArrangeParts);
				processingProgressControl.ProcessType = progressArrangePartsFull;
				processingProgressControl.Visible = true;
				processingProgressControl.PercentComplete = 0;
				LockEditControls();

				await Task.Run(() =>
				{
					Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
					PushMeshGroupDataToAsynchLists(TraceInfoOpperation.DONT_COPY);
					PlatingHelper.ArrangeMeshGroups(asynchMeshGroups, asynchMeshGroupTransforms, asynchPlatingDatas, ReportProgressChanged);
				});

				if (WidgetHasBeenClosed)
				{
					return;
				}

				// offset them to the centor of the bed
				for (int i = 0; i < asynchMeshGroups.Count; i++)
				{
					ScaleRotateTranslate translate = asynchMeshGroupTransforms[i];
					translate.translation *= Matrix4X4.CreateTranslation(new Vector3(ActiveSliceSettings.Instance.BedCenter, 0));
					asynchMeshGroupTransforms[i] = translate;
				}

				UnlockEditControls();
				PartHasBeenChanged();

				PullMeshGroupDataFromAsynchLists();
			}
		}
	}
}