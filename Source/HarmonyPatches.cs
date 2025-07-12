using HarmonyLib;
using Mono.Cecil.Cil;
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
			var harmony = new Harmony("syrus.colonistbaradjuster");

			harmony.Patch(
				typeof(ColonistBarDrawLocsFinder).GetMethod(nameof(ColonistBarDrawLocsFinder.FindBestScale), BindingFlags.Instance | BindingFlags.NonPublic,
					null, new Type[] { typeof(bool).MakeByRefType(), typeof(int).MakeByRefType(), typeof(int) }, null),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(ColonistBarDrawLocsFinder_FindBestScale_Prefix)));

			harmony.Patch(
				typeof(ColonistBarDrawLocsFinder).GetMethod(nameof(ColonistBarDrawLocsFinder.GetDrawLoc), BindingFlags.Instance | BindingFlags.NonPublic),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(ColonistMarginAdjustment_Transpiler)));
			harmony.Patch(
				typeof(ColonistBarDrawLocsFinder).GetMethod(nameof(ColonistBarDrawLocsFinder.CalculateDrawLocs), BindingFlags.Instance | BindingFlags.NonPublic,
					null, new Type[] { typeof(List<Vector2>), typeof(float), typeof(bool), typeof(int), typeof(int) }, null),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(ColonistMarginAdjustment_Transpiler)));

			harmony.Patch(
				typeof(ColonistBarColonistDrawer).GetMethod(nameof(ColonistBarColonistDrawer.DrawGroupFrame), BindingFlags.Instance | BindingFlags.Public),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(ColonistBarColonistDrawer_DrawGroupFrame_Transpiler)));

			harmony.Patch(
				AccessTools.Method(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawColonist)),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(ColonistBarColonistDrawer_DrawColonist_Transpiler)));
		}

		static bool ColonistBarDrawLocsFinder_FindBestScale_Prefix(ColonistBarDrawLocsFinder __instance, ref float __result, ref bool onlyOneRow, ref int maxPerGlobalRow, int groupsCount)
		{
			var scale = ColonistBarAdjuster.Settings.BaseScale;
			maxPerGlobalRow = ColonistBarAdjuster.Settings.ColonistsPerRow;
			var entries = __instance.ColonistBar.Entries;
			while (true)
			{
				onlyOneRow = true;

				if (__instance.TryDistributeHorizontalSlotsBetweenGroups(maxPerGlobalRow, groupsCount))
				{
					int allowedRowsCountForScale = ColonistBarAdjuster.Settings.MaxNumberOfRows;
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

				var widthPerColonist = (ColonistBar.BaseSize.x + ColonistBarAdjuster.Settings.MarginX) * scale;
				var totalWidth = ColonistBarDrawLocsFinder.MaxColonistBarWidth - (groupsCount - 1f) * 25f * scale; 
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
							baseMargin = ColonistBarAdjusterSettings.Default_MarginX;
							replacer = typeof(ColonistBarAdjuster).GetProperty(nameof(ColonistBarAdjuster.MarginX), BindingFlags.Static | BindingFlags.Public).GetGetMethod();
							break;
						case "y":
							baseMargin = ColonistBarAdjusterSettings.Default_MarginY;
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

		static IEnumerable<CodeInstruction> ColonistBarColonistDrawer_DrawColonist_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var added = false;
			foreach (var instruction in instructions)
			{
				yield return instruction;

				//    ldsfld UnityEngine.Vector2 ColonistBarAdjuster.TEST::PawnTextureSize
				// ++ call static System.Single ColonistBarAdjuster.ColonistBarAdjuster::get_BaseScale()
				// ++ call static UnityEngine.Vector2 UnityEngine.Vector2::op_Multiply(UnityEngine.Vector2 a, System.Single d)
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fi && fi.Name == nameof(ColonistBarColonistDrawer.PawnTextureSize))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ColonistBarAdjuster), nameof(ColonistBarAdjuster.BaseScale)));
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector2), "op_Multiply", new Type[] { typeof(Vector2), typeof(float) }));
					added = true;
				}
			}
			if (!added)
				Log.Error($"{nameof(ColonistBarAdjuster)}: failed to apply {nameof(ColonistBarColonistDrawer_DrawColonist_Transpiler)} patch");
		}
	}
}
