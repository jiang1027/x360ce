﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App
{
	public class ControlHelper
	{

		public static void ShowHideAndSelectGridRows(DataGridView grid, ToolStripDropDownButton button, string primaryKey = null, object selectItemKey = null)
		{
			var rows = grid.Rows.Cast<DataGridViewRow>().ToArray();
			var showEnabled = button.Text.Contains("Enabled");
			var showDisabled = button.Text.Contains("Disabled");
			// Check if row needs rebinding.
			var needRebinding = false;
			for (int i = 0; i < rows.Length; i++)
			{
				var item = (IDisplayName)rows[i].DataBoundItem;
				var visible = true;
				if (showEnabled)
					visible = (item.IsEnabled == true);
				if (showDisabled)
					visible = (item.IsEnabled == false);
				if (rows[i].Visible != visible)
				{
					needRebinding = true;
					break;
				}
			}
			// If there is no collmns to hide or show then...
			if (needRebinding)
			{
				var selection = selectItemKey == null
				? JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<object>(grid, primaryKey)
				: new List<object>() { selectItemKey };
				grid.CurrentCell = null;
				// Suspend Layout and CurrencyManager to avoid exceptions.
				grid.SuspendLayout();
				var cm = (CurrencyManager)grid.BindingContext[grid.DataSource];
				cm.SuspendBinding();
				// Reverse order to hide/show bottom records first..
				Array.Reverse(rows);
				for (int i = 0; i < rows.Length; i++)
				{
					var item = (IDisplayName)rows[i].DataBoundItem;
					var visible = true;
					if (showEnabled)
						visible = (item.IsEnabled == true);
					if (showDisabled)
						visible = (item.IsEnabled == false);
					if (rows[i].Visible != visible)
					{
						rows[i].Visible = visible;
					}
				}
				// Resume CurrencyManager and Layout.
				cm.ResumeBinding();
				grid.ResumeLayout();
				// Restore selection.
				JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection(grid, primaryKey, selection, true);
			}
			// If nothing is selected then...
			else if (grid.SelectedRows.Count == 0)
			{
				var firstVisibleRow = rows.FirstOrDefault(x => x.Visible);
				if (firstVisibleRow != null)
				{
					// Select first visible row.
					firstVisibleRow.Selected = true;
				}
			}
		}


		public static void LoadAndMonitor(Expression<Func<Options, object>> setting, Control control, object dataSource = null)
		{
			var o = SettingsManager.Options;
			SettingsManager.AddMap(setting, control);
			if (dataSource != null)
			{
				// Set ComboBox and attach event last, in order to prevent changing of original value.
				var lc = control as ListControl;
				if (lc != null)
					lc.DataSource = dataSource;
				var lb = control as ListBox;
				if (lb != null)
					lb.DataSource = dataSource;
			}
			// Load settings into control.
			var body = (setting.Body as MemberExpression)
				 ?? (((UnaryExpression)setting.Body).Operand as MemberExpression);
			var propertyName = body.Member.Name;
			o.OnPropertyChanged(propertyName);
			// Monitor control changes.
			var chb = control as CheckBox;
			if (chb != null)
				chb.CheckStateChanged += Control_Changed;
			var cbx = control as ComboBox;
			if (cbx != null)
			{
				cbx.TextChanged += Control_Changed;
				cbx.SelectedIndexChanged += Control_Changed;
			}
		}

		private static void Control_Changed(object sender, EventArgs e)
		{
			SettingsManager.Sync((Control)sender, SettingsManager.Options);
		}

	}
}
