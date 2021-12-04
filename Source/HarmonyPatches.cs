using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ColonistBarAdjuster
{
	[StaticConstructorOnStartup]
	public static class HarmonyPatches
	{
		static HarmonyPatches()
		{
			Harmony harmony = new Harmony("syrus.colonistbaradjuster");

			harmony.Patch(
				typeof(ColonistBarDrawLocsFinder).GetMethod("FindBestScale", BindingFlags.Instance | BindingFlags.NonPublic,
					null, new Type[] { typeof(bool).MakeByRefType(), typeof(int).MakeByRefType(), typeof(int) }, null),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.ColonistBarDrawLocsFinder_FindBestScale_Prefix)));

			harmony.Patch(
				typeof(ColonistBarDrawLocsFinder).GetMethod("GetDrawLoc", BindingFlags.Instance | BindingFlags.NonPublic),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.ColonistMarginAdjustment_Transpiler)));
			harmony.Patch(
				typeof(ColonistBarDrawLocsFinder).GetMethod("CalculateDrawLocs", BindingFlags.Instance | BindingFlags.NonPublic,
					null, new Type[] { typeof(List<Vector2>), typeof(float), typeof(bool), typeof(int), typeof(int) }, null),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.ColonistMarginAdjustment_Transpiler)));
		}

		public static bool ColonistBarDrawLocsFinder_FindBestScale_Prefix(ColonistBarDrawLocsFinder __instance, ref float __result, ref bool onlyOneRow, ref int maxPerGlobalRow, int groupsCount)
		{
			float scale = ColonistBarAdjuster.Instance.BaseScale;
			maxPerGlobalRow = ColonistBarAdjuster.Instance.ColonistsPerRow;
			List<ColonistBar.Entry> entries = __instance.ColonistBar.Entries;
			while (true)
			{
				onlyOneRow = true;

				if (__instance.TryDistributeHorizontalSlotsBetweenGroups(maxPerGlobalRow, groupsCount))
				{
					int allowedRowsCountForScale = ColonistBarAdjuster.Instance.MaxNumberOfRows;
					bool flag = true;
					int group = -1;
					for (int i = 0; i < entries.Count; i++)
					{
						if (group != entries[i].group)
						{
							group = entries[i].group;
							int groupRowCount = Mathf.CeilToInt(__instance.entriesInGroup[entries[i].group] / (float)__instance.horizontalSlotsPerGroup[entries[i].group]);
							if (groupRowCount > 1)
								onlyOneRow = false;
							if (groupRowCount > allowedRowsCountForScale)
							{
								flag = false;
								break;
							}
						}
					}
					if (flag)
						break;
				}

				// use standard RimWorld logic if we fail to create the colonist bar while respecting the limitations set in the settings (colonists per row & row count)
				scale *= 0.95f;

				float widthPerColonist = (ColonistBar.BaseSize.x + GetColonistMarginX()) * scale;
				float totalWidth = ColonistBarDrawLocsFinder.MaxColonistBarWidth - (groupsCount - 1f) * 25f * scale; 
				maxPerGlobalRow = Mathf.FloorToInt(totalWidth / widthPerColonist);
			}
			__result = scale;

			return false;
		}


		public static IEnumerable<CodeInstruction> ColonistMarginAdjustment_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			bool baseSizeFound = false;
			bool baseSizeXFound = false;
			bool baseSizeYFound = false;
			foreach (var instruction in instructions)
			{
				if (!baseSizeFound 
					&& instruction.opcode == OpCodes.Ldsflda 
					&& instruction.operand is FieldInfo fieldInfoBaseSize
					&& fieldInfoBaseSize.DeclaringType?.FullDescription() == "RimWorld.ColonistBar" 
					&& fieldInfoBaseSize.Name == "BaseSize")
				{
					//Log.Warning("FOUND 'RimWorld.ColonistBar.BaseSize'");
					baseSizeFound = true;
				}
				else if (baseSizeFound)
				{
					bool end = true;
					if (instruction.opcode == OpCodes.Ldfld
						&& instruction.operand is FieldInfo fieldInfoVector2
						&& fieldInfoVector2.DeclaringType?.FullDescription() == "UnityEngine.Vector2")
					{
						if (fieldInfoVector2.Name == "x")
						{
							//Log.Warning("FOUND 'UnityEngine.Vector2.x'");
							baseSizeXFound = true;
							end = false;
						}
						else if (fieldInfoVector2.Name == "y")
						{
							//Log.Warning("FOUND 'UnityEngine.Vector2.y'");
							baseSizeYFound = true;
							end = false;
						}
					}
					else if (instruction.opcode == OpCodes.Ldc_R4
						&& instruction.operand is float value)
					{
						if (baseSizeXFound && value == ColonistBarAdjuster.BaseMarginX)
						{
							//Log.Warning("REPLACING X MARGIN");
							instruction.opcode = OpCodes.Call;
							instruction.operand = GetColonistMarginXMethodInfo;
						}
						else if (baseSizeYFound && value == ColonistBarAdjuster.BaseMarginY)
						{
							//Log.Warning("REPLACING Y MARGIN");
							instruction.opcode = OpCodes.Call;
							instruction.operand = GetColonistMarginYMethodInfo;
						}
					}

					if (end)
					{
						//Log.Warning("END");
						baseSizeXFound = false;
						baseSizeYFound = false;
						baseSizeFound = false;
					}
				}

				//Log.Message(instruction.ToString());
				yield return instruction;
			}
		}

		private static readonly MethodInfo GetColonistMarginXMethodInfo = ((Func<float>)GetColonistMarginX).GetMethodInfo();
		private static float GetColonistMarginX() => ColonistBarAdjuster.Instance.MarginX;

		private static readonly MethodInfo GetColonistMarginYMethodInfo = ((Func<float>)GetColonistMarginY).GetMethodInfo();
		private static float GetColonistMarginY() => ColonistBarAdjuster.Instance.MarginY;
	}
}
