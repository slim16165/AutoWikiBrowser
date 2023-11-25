/*
Copyright (C) 2008 Stephen Kennedy <steve@sdk-software.com>

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using WikiFunctions.Plugin;
using System.Windows.Forms;

namespace AutoWikiBrowser;

partial class MainForm
{
    CheckBox IAutoWikiBrowserForm.BotModeCheckbox => chkAutoMode;
    CheckBox IAutoWikiBrowserForm.SkipNoChangesCheckBox => chkSkipNoChanges;
    Button IAutoWikiBrowserForm.PreviewButton => btnPreview;
    Button IAutoWikiBrowserForm.SaveButton => btnSave;
    Button IAutoWikiBrowserForm.SkipButton => btnIgnore;
    Button IAutoWikiBrowserForm.StopButton => btnStop;
    Button IAutoWikiBrowserForm.DiffButton => btnDiff;
    Button IAutoWikiBrowserForm.StartButton => btnStart;
    ComboBox IAutoWikiBrowserForm.EditSummaryComboBox => cmboEditSummary;
    StatusStrip IAutoWikiBrowserForm.StatusStrip => StatusMain;
    NotifyIcon IAutoWikiBrowserForm.NotifyIcon => ntfyTray;
    RadioButton IAutoWikiBrowserForm.SkipNonExistentPages => radSkipNonExistent;
    CheckBox IAutoWikiBrowserForm.ApplyGeneralFixesCheckBox => chkGeneralFixes;
    CheckBox IAutoWikiBrowserForm.AutoTagCheckBox => chkAutoTagger;
    CheckBox IAutoWikiBrowserForm.RegexTypoFix => chkRegExTypo;
    bool IAutoWikiBrowserForm.PreParseMode => preParseModeToolStripMenuItem.Checked;
    WikiFunctions.Controls.ArticleTextBox IAutoWikiBrowserForm.EditBox => txtEdit;
    Form IAutoWikiBrowserForm.Form => this;
    ToolStripMenuItem IAutoWikiBrowserForm.HelpToolStripMenuItem => helpToolStripMenuItem;
    ToolStripMenuItem IAutoWikiBrowserForm.PluginsToolStripMenuItem => pluginsToolStripMenuItem;
    ToolStripMenuItem IAutoWikiBrowserForm.InsertTagToolStripMenuItem => insertTagToolStripMenuItem;
    ToolStripMenuItem IAutoWikiBrowserForm.ToolStripMenuGeneral => ToolStripMenuGeneral;
    WikiFunctions.Controls.Lists.ListMaker IAutoWikiBrowserForm.ListMaker => listMaker;
    ContextMenuStrip IAutoWikiBrowserForm.EditBoxContextMenu => mnuTextBox;
    WikiFunctions.Logging.LogControl IAutoWikiBrowserForm.LogControl => logControl;

    string IAutoWikiBrowserForm.StatusLabelText { get => StatusLabelText;
        set => StatusLabelText = value;
    }
}