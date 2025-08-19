using SyControlsBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ColonistBarAdjuster
{
	public class ColonistBarAdjusterSettings : ModSettings
	{
		#region CONSTANTS
		public const float Default_OffsetX = 0f;
		public const float Default_OffsetY = 0f;
		public const float Default_BaseScale = 1f;
		public const int Default_ColonistsPerRow = 28;
		public const int Default_MaxNumberOfRows = 3;
		public const float Default_MarginX = 24f;
		public const float Default_MarginY = 32f;
		public const bool Default_HideBackground = false;
		#endregion

		#region PROPERTIES
		private float _offsetX = Default_OffsetX;
		public float OffsetX
		{
			get => _offsetX;
			set => Util.SetValue(ref _offsetX, value, v => ApplyChanges());
		}
		private float _offsetY = Default_OffsetY;
		public float OffsetY
		{
			get => _offsetY;
			set => Util.SetValue(ref _offsetY, value, v => ApplyChanges());
		}
		private float _baseScale = Default_BaseScale;
		public float BaseScale
		{
			get => _baseScale;
			set => Util.SetValue(ref _baseScale, value, v => ApplyChanges());
		}
		private int _colonistsPerRow = Default_ColonistsPerRow;
		public int ColonistsPerRow
		{
			get => _colonistsPerRow;
			set => Util.SetValue(ref _colonistsPerRow, value, v => ApplyChanges());
		}
		private int _maxNumberOfRows = Default_MaxNumberOfRows;
		public int MaxNumberOfRows
		{
			get => _maxNumberOfRows;
			set => Util.SetValue(ref _maxNumberOfRows, value, v => ApplyChanges());
		}
		private float _marginX = Default_MarginX;
		public float MarginX
		{
			get => _marginX;
			set => Util.SetValue(ref _marginX, value, v => ApplyChanges());
		}
		private float _marginY = Default_MarginY;
		public float MarginY
		{
			get => _marginY;
			set => Util.SetValue(ref _marginY, value, v => ApplyChanges());
		}
		private bool _hideBackground = Default_HideBackground;
		public bool HideBackground
		{
			get => _hideBackground;
			set => Util.SetValue(ref _hideBackground, value, v => ApplyChanges());
		}
		#endregion

		#region PUBLIC METHODS
		public void DoSettingsWindowContents(Rect inRect)
		{
			var width = inRect.width;
			var offsetY = 0.0f;

			ControlsBuilder.Begin(inRect);
			try
			{
				OffsetX = ControlsBuilder.CreateNumeric(
					ref offsetY,
					width,
					"SY_CBA.OffsetX".Translate(),
					"SY_CBA.OffsetXDesc".Translate(),
					OffsetX,
					Default_OffsetX,
					nameof(OffsetX),
					float.MinValue);
				OffsetY = ControlsBuilder.CreateNumeric(
					ref offsetY,
					width,
					"SY_CBA.OffsetY".Translate(),
					"SY_CBA.OffsetYDesc".Translate(),
					OffsetY,
					Default_OffsetY,
					nameof(OffsetY),
					float.MinValue);

				BaseScale = ControlsBuilder.CreateNumeric(
					ref offsetY,
					width,
					"SY_CBA.BaseScale".Translate(),
					"SY_CBA.BaseScaleDesc".Translate(),
					BaseScale,
					Default_BaseScale,
					nameof(BaseScale),
					0.02f,
					100f);

				ColonistsPerRow = ControlsBuilder.CreateNumeric(
					ref offsetY,
					width,
					"SY_CBA.ColonistsPerRow".Translate(),
					"SY_CBA.ColonistsPerRowDesc".Translate(),
					ColonistsPerRow,
					Default_ColonistsPerRow,
					nameof(ColonistsPerRow),
					1,
					1000);
				MaxNumberOfRows = ControlsBuilder.CreateNumeric(
					ref offsetY,
					width,
					"SY_CBA.MaxNumOfRows".Translate(),
					"SY_CBA.MaxNumOfRowsDesc".Translate(),
					MaxNumberOfRows,
					Default_MaxNumberOfRows,
					nameof(MaxNumberOfRows),
					1,
					10);

				MarginX = ControlsBuilder.CreateNumeric(
					ref offsetY,
					width,
					"SY_CBA.MarginX".Translate(),
					"SY_CBA.MarginXDesc".Translate(),
					MarginX,
					Default_MarginX,
					nameof(MarginX),
					1f,
					100f);
				MarginY = ControlsBuilder.CreateNumeric(
					ref offsetY,
					width,
					"SY_CBA.MarginY".Translate(),
					"SY_CBA.MarginYDesc".Translate(),
					MarginY,
					Default_MarginY,
					nameof(MarginY),
					1f,
					100f);

				HideBackground = ControlsBuilder.CreateCheckbox(
					ref offsetY,
					width,
					"SY_CBA.HideBackground".Translate(),
					"SY_CBA.HideBackgroundDesc".Translate(),
					HideBackground,
					Default_HideBackground);
			}
			finally
			{
				ControlsBuilder.End(offsetY);
			}
		}
		#endregion

		#region OVERRIDES
		public override void ExposeData()
		{
			base.ExposeData();

			var floatValue = OffsetX;
			Scribe_Values.Look(ref floatValue, nameof(OffsetX), Default_OffsetX);
			OffsetX = floatValue;
			floatValue = OffsetY;
			Scribe_Values.Look(ref floatValue, nameof(OffsetY), Default_OffsetY);
			OffsetY = floatValue;

			floatValue = BaseScale;
			Scribe_Values.Look(ref floatValue, nameof(BaseScale), Default_BaseScale);
			BaseScale = floatValue;

			var intValue = ColonistsPerRow;
			Scribe_Values.Look(ref intValue, nameof(ColonistsPerRow), Default_ColonistsPerRow);
			ColonistsPerRow = intValue;
			intValue = MaxNumberOfRows;
			Scribe_Values.Look(ref intValue, nameof(MaxNumberOfRows), Default_MaxNumberOfRows);
			MaxNumberOfRows = intValue;

			floatValue = MarginX;
			Scribe_Values.Look(ref floatValue, nameof(MarginX), Default_MarginX);
			MarginX = floatValue;
			floatValue = MarginY;
			Scribe_Values.Look(ref floatValue, nameof(MarginY), Default_MarginY);
			MarginY = floatValue;

			var boolValue = HideBackground;
			Scribe_Values.Look(ref boolValue, nameof(HideBackground), Default_HideBackground);
			HideBackground = boolValue;

			ApplyChanges();
		}
		#endregion

		#region PRIVATE METHODS
		private void ApplyChanges()
		{
			if (!GenScene.InPlayScene)
				return;

			var bar = Find.ColonistBar;
			if (bar?.Visible == true)
			{
				bar.entriesDirty = true;
				bar.CheckRecacheEntries();
			}
		}
		#endregion
	}
}
