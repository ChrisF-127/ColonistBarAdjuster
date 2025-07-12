using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ColonistBarAdjuster
{
	public class ColonistBarAdjuster : Mod
	{
		#region PROPERTIES
		public static ColonistBarAdjuster Instance { get; private set; }
		public static ColonistBarAdjusterSettings Settings { get; private set; }

		// Static properties for easier use, otherwise I'd have to rewrite the HarmonyPatches
		public static float MarginX => Settings.MarginX;
		public static float MarginY => Settings.MarginY;
		public static float OffsetX => Settings.OffsetX;
		public static float OffsetY => Settings.OffsetY;
		public static float BaseScale => Settings.BaseScale;
		public static bool HideBackground => Settings.HideBackground;
		#endregion

		#region CONSTRUCTORS
		public ColonistBarAdjuster(ModContentPack content) : base(content)
		{
			Instance = this;

			LongEventHandler.ExecuteWhenFinished(Initialize);
		}
		#endregion

		#region OVERRIDES
		public override string SettingsCategory() =>
			"Colonist Bar Adjuster";

		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);

			Settings.DoSettingsWindowContents(inRect);
		}
		#endregion

		#region PRIVATE METHODS
		private void Initialize()
		{
			Settings = GetSettings<ColonistBarAdjusterSettings>();
		}
		#endregion
	}
}
