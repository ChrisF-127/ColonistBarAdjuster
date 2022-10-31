using HugsLib.Settings;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ColonistBarAdjuster
{
	public class ColonistBarAdjuster : HugsLib.ModBase
	{
		public override string ModIdentifier => "ColonistBarAdjuster";

		public const float BaseOffsetX = 0f;
		public const float BaseOffsetY = 0f;
		public const float BaseMarginX = 24f;
		public const float BaseMarginY = 32f;

		private SettingHandle<float> _offsetX;
		public static float OffsetX { get; private set; }
		private SettingHandle<float> _offsetY;
		public static float OffsetY { get; private set; }
		private SettingHandle<float> _baseScale;
		public static float BaseScale { get; private set; }
		private SettingHandle<int> _colonistsPerRow;
		public static int ColonistsPerRow { get; private set; }
		private SettingHandle<int> _maxNumberOfRows;
		public static int MaxNumberOfRows { get; private set; }
		private SettingHandle<float> _marginX;
		public static float MarginX { get; private set; }
		private SettingHandle<float> _marginY;
		public static float MarginY { get; private set; }
		private SettingHandle<bool> _hideBackground;
		public static bool HideBackground { get; private set; }

		public override void DefsLoaded()
		{
			_offsetX = Settings.GetHandle(
				"offsetX",
				"SY_CBA.OffsetX".Translate(),
				"SY_CBA.OffsetXDesc".Translate(),
				BaseOffsetX);
			_offsetX.ValueChanged += val => ValueChanged();

			_offsetY = Settings.GetHandle(
				"offsetY",
				"SY_CBA.OffsetY".Translate(),
				"SY_CBA.OffsetYDesc".Translate(),
				BaseOffsetY);
			_offsetY.ValueChanged += val => ValueChanged();

			_baseScale = Settings.GetHandle(
				"baseScale",
				"SY_CBA.BaseScale".Translate(),
				"SY_CBA.BaseScaleDesc".Translate(),
				1f,
				Validators.FloatRangeValidator(0.01f, 100f));
			_baseScale.ValueChanged += val => ValueChanged();

			_colonistsPerRow = Settings.GetHandle(
				"colonistsPerRow",
				"SY_CBA.ColonistsPerRow".Translate(),
				"SY_CBA.ColonistsPerRowDesc".Translate(),
				20,
				Validators.IntRangeValidator(1, 1000));
			_colonistsPerRow.ValueChanged += val => ValueChanged();

			_maxNumberOfRows = Settings.GetHandle(
				"maxNumberOfRows",
				"SY_CBA.MaxNumOfRows".Translate(),
				"SY_CBA.MaxNumOfRowsDesc".Translate(),
				3,
				Validators.IntRangeValidator(1, 10));
			_maxNumberOfRows.ValueChanged += val => ValueChanged();

			_marginX = Settings.GetHandle(
				"marginX",
				"SY_CBA.MarginX".Translate(),
				"SY_CBA.MarginXDesc".Translate(),
				BaseMarginX,
				Validators.FloatRangeValidator(0f, 100f));
			_marginX.ValueChanged += val => ValueChanged();

			_marginY = Settings.GetHandle(
				"marginY",
				"SY_CBA.MarginY".Translate(),
				"SY_CBA.MarginYDesc".Translate(),
				BaseMarginY,
				Validators.FloatRangeValidator(0f, 100f));
			_marginY.ValueChanged += val => ValueChanged();

			_hideBackground = Settings.GetHandle(
				"hideBackground",
				"SY_CBA.HideBackground".Translate(),
				"SY_CBA.HideBackgroundDesc".Translate(),
				HideBackground);
			_hideBackground.ValueChanged += val => ValueChanged();

			ValueChanged();
		}

		private void ValueChanged()
		{
			OffsetX = _offsetX;
			OffsetY = _offsetY;
			BaseScale = _baseScale;
			ColonistsPerRow = _colonistsPerRow;
			MaxNumberOfRows = _maxNumberOfRows;
			MarginX = _marginX;
			MarginY = _marginY;
			HideBackground = _hideBackground;

			if (GenScene.InPlayScene)
			{
				var bar = Find.ColonistBar;
				if (bar?.Visible == true)
				{
					bar.entriesDirty = true;
					bar.CheckRecacheEntries();
				}
			}
		}
	}
}
