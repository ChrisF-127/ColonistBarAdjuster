﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace SyControlsBuilder
{
	public static class ControlsBuilder
	{
		#region DELEGATE
		public delegate string ConvertDelegate<T>(T value);
		#endregion

		#region FIELDS
		private static readonly Dictionary<string, string> ValueBuffers = new Dictionary<string, string>();

		private static readonly Color ModifiedColor = Color.cyan;

		private static GameFont OriTextFont;
		private static TextAnchor OriTextAnchor;
		private static Color OriColor;

		private const float SettingsRowHeight = 32f;
		#endregion

		#region PUBLIC METHODS
		public static void Begin(Rect inRect)
		{
			OriTextFont = Text.Font;
			OriTextAnchor = Text.Anchor;
			OriColor = GUI.color;

			GUI.BeginGroup(inRect);
		}
		public static void End()
		{
			GUI.EndGroup();

			Text.Font = OriTextFont;
			Text.Anchor = OriTextAnchor;
			GUI.color = OriColor;
		}

		public static T CreateNumeric<T>(
			ref float offsetY,
			float viewWidth,
			string label,
			string tooltip,
			T value,
			T defaultValue,
			string valueBufferKey,
			float min = 0f,
			float max = 1e+9f,
			ConvertDelegate<T> additionalText = null,
			string unit = null)
			where T : struct, IComparable
		{
			var isModified = !value.Equals(defaultValue);
			var controlWidth = GetControlWidth(viewWidth);

			Text.Anchor = TextAnchor.MiddleLeft;

			// Label
			if (isModified)
				GUI.color = ModifiedColor;
			Widgets.Label(new Rect(0, offsetY, controlWidth - 8, SettingsRowHeight), label);
			GUI.color = OriColor;

			// Setting
			var textFieldRect = new Rect(controlWidth + 2, offsetY + 6, controlWidth / 2 - 4, SettingsRowHeight - 12);
			var valueBuffer = GetOrAddDefault(ValueBuffers, valueBufferKey);
			Widgets.TextFieldNumeric(textFieldRect, ref value, ref valueBuffer, min, max);
			ValueBuffers[valueBufferKey] = valueBuffer;
			if (!string.IsNullOrWhiteSpace(tooltip))
				DrawTooltip(textFieldRect, tooltip);

			// Unit
			DrawTextFieldUnit(textFieldRect, unit);

			// Additional Text
			if (additionalText != null)
			{
				var additionalTextRect = textFieldRect;
				additionalTextRect.x += textFieldRect.width + 8;
				additionalTextRect.width -= 8;
				Widgets.Label(additionalTextRect, additionalText(value));
			}

			// Reset button
			if (isModified && DrawResetButton(offsetY, viewWidth, defaultValue.ToString()))
			{
				value = defaultValue;
				ValueBuffers.Remove(valueBufferKey);
			}

			offsetY += SettingsRowHeight;
			return value;
		}

		public static bool CreateCheckbox(
			ref float offsetY,
			float viewWidth,
			string label,
			string tooltip,
			bool value,
			bool defaultValue,
			string text = null)
		{
			var controlWidth = GetControlWidth(viewWidth);
			var isModified = value != defaultValue;

			// Label
			if (isModified)
				GUI.color = ModifiedColor;
			Widgets.Label(new Rect(0, offsetY, controlWidth, SettingsRowHeight), label);
			GUI.color = OriColor;

			// Setting
			var checkboxSize = SettingsRowHeight - 8;
			Widgets.Checkbox(controlWidth, offsetY + (SettingsRowHeight - checkboxSize) / 2, ref value, checkboxSize);
			DrawTooltip(new Rect(controlWidth, offsetY, checkboxSize, checkboxSize), tooltip);

			// Text
			if (text != null)
				Widgets.Label(new Rect(controlWidth + checkboxSize + 4, offsetY + 4, controlWidth - checkboxSize - 6, SettingsRowHeight - 8), text ?? "");

			// Reset button
			if (isModified && DrawResetButton(offsetY, viewWidth, defaultValue.ToString()))
				value = defaultValue;

			offsetY += SettingsRowHeight;
			return value;
		}

		public static bool DrawResetButton(float offsetY, float viewWidth, string tooltip)
		{
			var buttonRect = new Rect(viewWidth + 2 - (SettingsRowHeight * 2), offsetY + 2, SettingsRowHeight * 2 - 4, SettingsRowHeight - 4);
			DrawTooltip(buttonRect, "Reset to " + tooltip);
			return Widgets.ButtonText(buttonRect, "Reset");
		}
		public static void DrawTooltip(Rect rect, string tooltip)
		{
			if (Mouse.IsOver(rect))
			{
				ActiveTip activeTip = new ActiveTip(tooltip);
				activeTip.DrawTooltip(GenUI.GetMouseAttachedWindowPos(activeTip.TipRect.width, activeTip.TipRect.height) + (UI.MousePositionOnUIInverted - Event.current.mousePosition));
			}
		}
		public static void DrawTextFieldUnit(Rect rect, string text)
		{
			Text.Anchor = TextAnchor.MiddleRight;
			Widgets.Label(new Rect(rect.x + 4, rect.y + 1, rect.width - 8, rect.height), text);
			Text.Anchor = TextAnchor.MiddleLeft;
		}

		public static float GetControlWidth(float viewWidth) =>
			viewWidth / 2 - SettingsRowHeight - 4;
		#endregion

		#region PRIVATE METHODS
		private static TValue GetOrAddDefault<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
		{
			if (dictionary.TryGetValue(key, out TValue value))
				return value;
			dictionary.Add(key, defaultValue);
			return defaultValue;
		}
		#endregion
	}
}
