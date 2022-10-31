using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
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
				typeof(ColonistBarDrawLocsFinder).GetMethod(nameof(ColonistBarDrawLocsFinder.FindBestScale), BindingFlags.Instance | BindingFlags.NonPublic,
					null, new Type[] { typeof(bool).MakeByRefType(), typeof(int).MakeByRefType(), typeof(int) }, null),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.ColonistBarDrawLocsFinder_FindBestScale_Prefix)));

			harmony.Patch(
				typeof(ColonistBarDrawLocsFinder).GetMethod(nameof(ColonistBarDrawLocsFinder.GetDrawLoc), BindingFlags.Instance | BindingFlags.NonPublic),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.ColonistMarginAdjustment_Transpiler)));
			harmony.Patch(
				typeof(ColonistBarDrawLocsFinder).GetMethod(nameof(ColonistBarDrawLocsFinder.CalculateDrawLocs), BindingFlags.Instance | BindingFlags.NonPublic,
					null, new Type[] { typeof(List<Vector2>), typeof(float), typeof(bool), typeof(int), typeof(int) }, null),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.ColonistMarginAdjustment_Transpiler)));

			harmony.Patch(
				typeof(ColonistBarColonistDrawer).GetMethod(nameof(ColonistBarColonistDrawer.DrawGroupFrame), BindingFlags.Instance | BindingFlags.Public),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.ColonistBarColonistDrawer_DrawGroupFrame_Transpiler)));

			//harmony.Patch(
			//	typeof(Test).GetMethod("GetDrawLoc", BindingFlags.Instance | BindingFlags.Public),
			//	transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Transpiler)));
			//harmony.Patch(
			//	typeof(Test).GetMethod("GetDrawLoc1", BindingFlags.Instance | BindingFlags.Public),
			//	transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Transpiler)));
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
		{
			Log.Message(__originalMethod.Name);
			foreach (var instruction in instructions)
			{
				Log.Message(instruction.ToString());
				yield return instruction;
			}
		}

		static bool ColonistBarDrawLocsFinder_FindBestScale_Prefix(ColonistBarDrawLocsFinder __instance, ref float __result, ref bool onlyOneRow, ref int maxPerGlobalRow, int groupsCount)
		{
			float scale = ColonistBarAdjuster.BaseScale;
			maxPerGlobalRow = ColonistBarAdjuster.ColonistsPerRow;
			List<ColonistBar.Entry> entries = __instance.ColonistBar.Entries;
			while (true)
			{
				onlyOneRow = true;

				if (__instance.TryDistributeHorizontalSlotsBetweenGroups(maxPerGlobalRow, groupsCount))
				{
					int allowedRowsCountForScale = ColonistBarAdjuster.MaxNumberOfRows;
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

				float widthPerColonist = (ColonistBar.BaseSize.x + ColonistBarAdjuster.MarginX) * scale;
				float totalWidth = ColonistBarDrawLocsFinder.MaxColonistBarWidth - (groupsCount - 1f) * 25f * scale; 
				maxPerGlobalRow = Mathf.FloorToInt(totalWidth / widthPerColonist);
			}
			__result = scale;

			return false;
		}


		static IEnumerable<CodeInstruction> ColonistMarginAdjustment_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
		{
			var list = instructions.ToList();

			// margin functions
			bool IsBaseSize(CodeInstruction instruction) =>
				instruction.opcode == OpCodes.Ldsflda
					&& instruction.operand is FieldInfo fieldInfoBaseSize
					&& fieldInfoBaseSize.DeclaringType?.FullDescription() == "RimWorld.ColonistBar" 
					&& fieldInfoBaseSize.Name == "BaseSize";
			string IsVectorXorY(CodeInstruction instruction) =>
				instruction.opcode == OpCodes.Ldfld
					&& instruction.operand is FieldInfo fieldInfoVector2
					&& fieldInfoVector2.DeclaringType?.FullDescription() == "UnityEngine.Vector2"
					? fieldInfoVector2.Name 
					: null;
			bool IsMargin(CodeInstruction instruction, float baseMargin) =>
				instruction.opcode == OpCodes.Ldc_R4 
				&& instruction.operand is float value
				&& value == baseMargin;

			// offset functions
			bool isGetDrawLoc = __originalMethod.Name == nameof(ColonistBarDrawLocsFinder.GetDrawLoc);
			bool IsVectorCtor(CodeInstruction instruction) =>
				instruction.opcode == OpCodes.Newobj
				&& (Type)instruction.operand.GetType().GetProperty("DeclaringType").GetValue(instruction.operand) == typeof(Vector2)
				&& (string)instruction.operand.GetType().GetProperty("Name").GetValue(instruction.operand) == ".ctor";

			for (int i = 0; i < list.Count; i++)
			{
				// margins
				if (i < list.Count - 3
					&& IsBaseSize(list[i])
					&& IsVectorXorY(list[i + 1]) is string v)
				{
					float baseMargin;
					MethodInfo replacer;
					switch (v)
					{
						case "x":
							baseMargin = ColonistBarAdjuster.BaseMarginX;
							replacer = typeof(ColonistBarAdjuster).GetProperty(nameof(ColonistBarAdjuster.MarginX), BindingFlags.Static | BindingFlags.Public).GetGetMethod();
							break;
						case "y":
							baseMargin = ColonistBarAdjuster.BaseMarginY;
							replacer = typeof(ColonistBarAdjuster).GetProperty(nameof(ColonistBarAdjuster.MarginY), BindingFlags.Static | BindingFlags.Public).GetGetMethod();
							break;
						default:
							continue;
					}
					i += 2;
					if (IsMargin(list[i], baseMargin))
					{
						list[i].opcode = OpCodes.Call;
						list[i].operand = replacer;
					}
				}

				// offsets
				if (isGetDrawLoc
					&& i < list.Count - 2
					&& list[i].opcode == OpCodes.Ldloc_1
					&& IsVectorCtor(list[i + 1]))
				{
					list.Insert(i, new CodeInstruction(OpCodes.Call, typeof(ColonistBarAdjuster).GetProperty(nameof(ColonistBarAdjuster.OffsetX), BindingFlags.Static | BindingFlags.Public).GetGetMethod()));
					i++; // skip Call
					list.Insert(i, new CodeInstruction(OpCodes.Add));
					i += 2; // skip Add and Ldloc_1

					list.Insert(i, new CodeInstruction(OpCodes.Call, typeof(ColonistBarAdjuster).GetProperty(nameof(ColonistBarAdjuster.OffsetY), BindingFlags.Static | BindingFlags.Public).GetGetMethod()));
					i++; // skip Call
					list.Insert(i, new CodeInstruction(OpCodes.Add));
					i += 2; // skip Add and Newobj
				}
			}

			//foreach (var instruction in list)
			//	Log.Message(instruction.ToString());

			return list;
		}

		static IEnumerable<CodeInstruction> ColonistBarColonistDrawer_DrawGroupFrame_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var endLabel = new Label();

			var addedInstruction = new CodeInstruction(OpCodes.Call, typeof(ColonistBarAdjuster).GetProperty(nameof(ColonistBarAdjuster.HideBackground), BindingFlags.Static | BindingFlags.Public).GetGetMethod());
			//Log.Warning(addedInstruction.ToString());
			yield return addedInstruction;

			addedInstruction = new CodeInstruction(OpCodes.Brtrue, endLabel);
			//Log.Warning(addedInstruction.ToString());
			yield return addedInstruction;

			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ret)
					instruction.labels.Add(endLabel);
				//Log.Message(instruction.ToString());
				yield return instruction;
			}
		}
	}

	public class Test
	{
		private List<int> horizontalSlotsPerGroup = new List<int>();
		private List<int> entriesInGroup = new List<int>();

		public Vector2 GetDrawLoc(float groupStartX, float groupStartY, int group, int numInGroup, float scale)
		{
			float num = groupStartX + (float)(numInGroup % horizontalSlotsPerGroup[group]) * scale * (ColonistBar.BaseSize.x + 24f);
			float y = groupStartY + (float)(numInGroup / horizontalSlotsPerGroup[group]) * scale * (ColonistBar.BaseSize.y + 32f);
			if (numInGroup >= entriesInGroup[group] - entriesInGroup[group] % horizontalSlotsPerGroup[group])
			{
				int num2 = horizontalSlotsPerGroup[group] - entriesInGroup[group] % horizontalSlotsPerGroup[group];
				num += (float)num2 * scale * (ColonistBar.BaseSize.x + 24f) * 0.5f;
			}
			return new Vector2(num, y);
		}

		public Vector2 GetDrawLoc1(float groupStartX, float groupStartY, int group, int numInGroup, float scale)
		{
			float num = groupStartX + (float)(numInGroup % horizontalSlotsPerGroup[group]) * scale * (ColonistBar.BaseSize.x + 24f);
			float y = groupStartY + (float)(numInGroup / horizontalSlotsPerGroup[group]) * scale * (ColonistBar.BaseSize.y + 32f);
			if (numInGroup >= entriesInGroup[group] - entriesInGroup[group] % horizontalSlotsPerGroup[group])
			{
				int num2 = horizontalSlotsPerGroup[group] - entriesInGroup[group] % horizontalSlotsPerGroup[group];
				num += (float)num2 * scale * (ColonistBar.BaseSize.x + 24f) * 0.5f;
			}
			return new Vector2(num + ColonistBarAdjuster.OffsetX, y + ColonistBarAdjuster.OffsetY);
		}
	}
}
