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
		public static ColonistBarAdjuster Instance { get; private set; }

		public override string ModIdentifier => "ColonistBarAdjuster";

		public const float BaseMarginX = 24f;
		public const float BaseMarginY = 32f;

		private SettingHandle<float> _baseScale;
		public float BaseScale { get; private set; }
		private SettingHandle<int> _colonistsPerRow;
		public int ColonistsPerRow { get; private set; }
		private SettingHandle<int> _maxNumberOfRows;
		public int MaxNumberOfRows { get; private set; }
		private SettingHandle<float> _marginX;
		public float MarginX { get; private set; }
		private SettingHandle<float> _marginY;
		public float MarginY { get; private set; }

		public ColonistBarAdjuster()
		{
			Instance = this;
		}

		public override void DefsLoaded()
		{
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

			ValueChanged();
		}

		private void ValueChanged()
		{
			BaseScale = _baseScale;
			ColonistsPerRow = _colonistsPerRow;
			MaxNumberOfRows = _maxNumberOfRows;
			MarginX = _marginX;
			MarginY = _marginY;

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
