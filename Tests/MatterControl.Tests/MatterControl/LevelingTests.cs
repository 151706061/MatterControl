﻿/*
Copyright (c) 2016, Lars Brubaker, John Lewin
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

using MatterHackers.Agg;
using MatterHackers.Agg.PlatformAbstract;
using MatterHackers.GCodeVisualizer;
using MatterHackers.MatterControl.ConfigurationPage.PrintLeveling;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.MatterControl.Tests.Automation;
using MatterHackers.VectorMath;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace MatterControl.Tests.MatterControl
{
	[TestFixture]
	public class LevelingTests
	{
		static LevelingTests()
		{
			MatterControlUtilities.OverrideAppDataLocation();
		}

		[Test, Category("Leveling")]
		public void Leveling7PointsNeverGetsTooHigh()
		{
			StaticData.Instance = new MatterHackers.Agg.FileSystemStaticData(Path.Combine("..", "..", "..", "..", "StaticData"));

			MatterControlUtilities.OverrideAppDataLocation();

			var levelingData = new PrintLevelingData(ActiveSliceSettings.Instance);

			double radius = 100;
			levelingData.SampledPositions = new List<Vector3>();
			levelingData.SampledPositions.Add(new Vector3(130.00, 0.00, 0));
			levelingData.SampledPositions.Add(new Vector3(65.00, 112.58, 10));
			levelingData.SampledPositions.Add(new Vector3(-65.00, 112.58, 0));
			levelingData.SampledPositions.Add(new Vector3(-130.00, 0.00, 10));
			levelingData.SampledPositions.Add(new Vector3(-65.00, -112.58, 0));
			levelingData.SampledPositions.Add(new Vector3(65.00, -112.58, 10));

			levelingData.SampledPositions.Add(new Vector3(0, 0, 0));

			levelingData.SampledPositions.Add(new Vector3(0, 0, 6));

			Vector2 bedCenter = Vector2.Zero;

			RadialLevlingFunctions levelingFunctions7Point = new RadialLevlingFunctions(6, levelingData, bedCenter);
			int totalPoints = 2000;
			for (int curPoint = 0; curPoint < totalPoints; curPoint++)
			{
				Vector2 currentTestPoint = new Vector2(radius, 0);
				currentTestPoint.Rotate(MathHelper.Tau / totalPoints * curPoint);
				Vector3 destPosition = new Vector3(currentTestPoint, 0);

				Vector3 outPosition = levelingFunctions7Point.GetPositionWithZOffset(destPosition);
				Assert.IsTrue(outPosition.z <= 10);

				string outPositionString = levelingFunctions7Point.DoApplyLeveling(GetGCodeString(destPosition), destPosition, PrinterMachineInstruction.MovementTypes.Absolute);
				double outZ = 0;
				Assert.IsTrue(GCodeFile.GetFirstNumberAfter("Z", outPositionString, ref outZ));
				Assert.IsTrue(outZ <= 10);
			}
		}

		[Test, Category("Leveling")]
		public void Leveling7PointsCorectInterpolation()
		{
			StaticData.Instance = new MatterHackers.Agg.FileSystemStaticData(Path.Combine("..", "..", "..", "..", "StaticData"));

			MatterControlUtilities.OverrideAppDataLocation();

			var levelingData = new PrintLevelingData(ActiveSliceSettings.Instance);

			double radius = 100;
			levelingData.SampledPositions = new List<Vector3>();
			Vector2 currentEdgePoint = new Vector2(radius, 0);
			for (int i = 0; i < 6; i++)
			{
				levelingData.SampledPositions.Add(new Vector3(currentEdgePoint, i));
				currentEdgePoint.Rotate(MathHelper.Tau / 6);
			}

			levelingData.SampledPositions.Add(new Vector3(0, 0, 6));

			Vector2 bedCenter = Vector2.Zero;

			RadialLevlingFunctions levelingFunctions7Point = new RadialLevlingFunctions(6, levelingData, bedCenter);
			for (int curPoint = 0; curPoint < 6; curPoint++)
			{
				int nextPoint = curPoint < 5 ? curPoint + 1 : 0;

				// test actual sample position
				Vector2 currentTestPoint = new Vector2(radius, 0);
				currentTestPoint.Rotate(MathHelper.Tau / 6 * curPoint);
				Vector3 destPosition = new Vector3(currentTestPoint, 0);
				Vector3 outPosition = levelingFunctions7Point.GetPositionWithZOffset(destPosition);
				Assert.AreEqual(outPosition.z, levelingData.SampledPositions[curPoint].z, .001);
				string outPositionString = levelingFunctions7Point.DoApplyLeveling(GetGCodeString(destPosition), destPosition, PrinterMachineInstruction.MovementTypes.Absolute);
				Assert.AreEqual(GetGCodeString(outPosition), outPositionString);

				// test mid point between samples
				Vector3 midPoint = (levelingData.SampledPositions[curPoint] + levelingData.SampledPositions[nextPoint]) / 2;
				currentTestPoint = new Vector2(midPoint.x, midPoint.y);
				destPosition = new Vector3(currentTestPoint, 0);
				outPosition = levelingFunctions7Point.GetPositionWithZOffset(destPosition);
				Assert.AreEqual(outPosition.z, midPoint.z, .001);
				outPositionString = levelingFunctions7Point.DoApplyLeveling(GetGCodeString(destPosition), destPosition, PrinterMachineInstruction.MovementTypes.Absolute);
				Assert.AreEqual(GetGCodeString(outPosition), outPositionString);

				// test mid point between samples with offset
				Vector3 midPointWithOffset = (levelingData.SampledPositions[curPoint] + levelingData.SampledPositions[nextPoint]) / 2 + new Vector3(0, 0, 3);
				currentTestPoint = new Vector2(midPointWithOffset.x, midPointWithOffset.y);
				destPosition = new Vector3(currentTestPoint, 3);
				outPosition = levelingFunctions7Point.GetPositionWithZOffset(destPosition);
				Assert.AreEqual(outPosition.z, midPointWithOffset.z, .001);
				outPositionString = levelingFunctions7Point.DoApplyLeveling(GetGCodeString(destPosition), destPosition, PrinterMachineInstruction.MovementTypes.Absolute);
				Assert.AreEqual(GetGCodeString(outPosition), outPositionString);

				// test 1/2 angles (mid way between samples on radius)
				currentTestPoint = new Vector2(radius, 0);
				currentTestPoint.Rotate(MathHelper.Tau / 6 * (curPoint + .5));
				destPosition = new Vector3(currentTestPoint, 0);
				outPosition = levelingFunctions7Point.GetPositionWithZOffset(destPosition);
				// the center is the higest point so the point on the radius has to be less than the mid point of the sample points (it is lower)
				Assert.IsTrue(outPosition.z < (levelingData.SampledPositions[curPoint].z + levelingData.SampledPositions[nextPoint].z) / 2 - .001);
				outPositionString = levelingFunctions7Point.DoApplyLeveling(GetGCodeString(destPosition), destPosition, PrinterMachineInstruction.MovementTypes.Absolute);
				Assert.AreEqual(GetGCodeString(outPosition), outPositionString);

				// test 1/2 to center
				currentTestPoint = new Vector2(radius / 2, 0);
				currentTestPoint.Rotate(MathHelper.Tau / 6 * curPoint);
				destPosition = new Vector3(currentTestPoint, 0);
				outPosition = levelingFunctions7Point.GetPositionWithZOffset(destPosition);
				Assert.AreEqual(outPosition.z, (levelingData.SampledPositions[curPoint].z + levelingData.SampledPositions[6].z) / 2, .001);
				outPositionString = levelingFunctions7Point.DoApplyLeveling(GetGCodeString(destPosition), destPosition, PrinterMachineInstruction.MovementTypes.Absolute);
				Assert.AreEqual(GetGCodeString(outPosition), outPositionString);
			}

			// prove that relative offsets work
			{
				Vector2 prevTestPoint = new Vector2(radius, 0);
				Vector3 prevDestPosition = new Vector3(prevTestPoint, 0);
				Vector3 prevOutPosition = levelingFunctions7Point.GetPositionWithZOffset(prevDestPosition);
				string prevOutPositionString = levelingFunctions7Point.DoApplyLeveling(GetGCodeString(prevDestPosition), prevDestPosition, PrinterMachineInstruction.MovementTypes.Absolute);

				for (int curPoint = 1; curPoint < 6; curPoint++)
				{
					// test actual sample position
					Vector2 currentTestPoint = new Vector2(radius, 0);
					currentTestPoint.Rotate(MathHelper.Tau / 6 * curPoint);
					Vector3 destPosition = new Vector3(currentTestPoint, 0);
					Vector3 outPosition = levelingFunctions7Point.GetPositionWithZOffset(destPosition);

					string outPositionString = levelingFunctions7Point.DoApplyLeveling(GetGCodeString(destPosition), destPosition, PrinterMachineInstruction.MovementTypes.Relative);
					Vector3 delatFromPrevToCurrent = outPosition - prevOutPosition;
					Assert.AreEqual(GetGCodeString(delatFromPrevToCurrent), outPositionString);

					prevTestPoint = currentTestPoint;
					prevDestPosition = destPosition;
					prevOutPosition = outPosition;
				}
			}

			Vector3 outPosition2 = levelingFunctions7Point.GetPositionWithZOffset(Vector3.Zero);
			Assert.AreEqual(outPosition2.z, levelingData.SampledPositions[6].z, .001);
		}

		private string GetGCodeString(Vector3 destPosition)
		{
			return "G1 X{0:0.##} Y{1:0.##} Z{2:0.###}".FormatWith(destPosition.x, destPosition.y, destPosition.z);
		}

		// TODO: do all the same tests for 13 point leveling.
	}
}
