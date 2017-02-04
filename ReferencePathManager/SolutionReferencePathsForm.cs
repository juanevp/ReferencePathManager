using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using ReferencePathManager.Properties;

namespace ReferencePathManager
{
	public partial class SolutionReferencePathsForm : Form
	{
		public SolutionReferencePathsForm(DTE2 applicationObject)
		{
			this.applicationObject = applicationObject;
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			foreach (Project proj in applicationObject.Solution.Projects)
			{
				var props = proj.Properties;
				if (props == null || proj.Kind != "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" /*PrjKind.prjKindCSharpProject*/ && proj.Kind != "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}" /*PrjKind.prjKindVBProject*/)
					continue;
				var values = new List<string>();
				paths[proj] = values;
				Property prop = null;
				for(var i = 1; i <= props.Count; i++)
				{
					var p = props.Item(i);
					if (p.Name == "ReferencePath")
					{
						prop = p;
						break;
					}
				}
				if (prop == null)
					continue;
				pathProps[proj] = prop;
				var value = (string)prop.Value;
				if (!string.IsNullOrEmpty(value))
					values.AddRange(value.Split(';'));
			}
			foreach (var proj in paths.Keys)
			{
				ProjectsLv.Items.Add(new ListViewItem(proj.Name){Tag = proj});
			}

		    this.KeyPreview = true;
            this.KeyDown += SolutionReferencePathsForm_KeyDown;
		}

        private void SolutionReferencePathsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control)
            {
                HandleAddFromClipboard();
            }
        }

        private void ProjectsLv_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
			RefreshPathsLv();
		}

		private void RefreshPathsLv()
		{
			var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			var selected = ProjectsLv.Items.Cast<ListViewItem>().Where(s => s.Selected).ToList();
			foreach (var item in selected)
			{
				var proj = (Project)item.Tag;
				List<string> pathlist;
				if (paths.TryGetValue(proj, out pathlist))
				{
					foreach (var path in pathlist)
					{
						int count;
						dict.TryGetValue(path, out count);
						dict[path] = ++count;
					}
				}
			}
			PathsLv.Items.Clear();
			foreach (var path in dict.Keys.OrderBy(s => s))
			{
				var item = new ListViewItem(path);
				if (dict[path] < selected.Count)
					item.ForeColor = Color.Gray;
				PathsLv.Items.Add(item);
			}
		}
		private void AcceptBtn_Click(object sender, EventArgs e)
		{
			foreach (var proj in paths.Keys)
			{
				var prop = pathProps[proj];
				var path = string.Join(";", paths[proj].ToArray());
				prop.Value = path;
			}
		}
		private void DeleteButton_Click(object sender, EventArgs e)
		{
			DeletePaths(GetSelectedProjects(), GetSelectedPaths());
			RefreshPathsLv();
		}
		private void DeleteAllButton_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show(Resources.InfoRemoveAllPaths, null, MessageBoxButtons.OKCancel) != DialogResult.OK) 
				return;
			DeletePaths(paths.Keys, GetSelectedPaths());
			RefreshPathsLv();
		}
		private void AddButton_Click(object sender, EventArgs e)
		{
			if (FolderBrowserDialog.ShowDialog() != DialogResult.OK)
				return;
			AppendPath(GetSelectedProjects(), FolderBrowserDialog.SelectedPath);
			RefreshPathsLv();
		}

	    private void HandleAddFromClipboard(object sender, EventArgs e) => AddPathFromClipboard();

	    private void HandleAddFromClipboard() => AddPathFromClipboard();

        private void AddPathFromClipboard()
        {
            var path = ClipboardManager.GetTextFromClipboard();
            if (String.IsNullOrEmpty(path))
            {
                ShowErrorMsgBox(Resources.ErrorClipboardContainsNoText);
                return;
            }

            if (!IsPathValid(path))
            {
                ShowErrorMsgBox(String.Format(Resources.ErrorInvalidPath, path));
                return;
            }

            AppendPath(GetSelectedProjects(), path);
            RefreshPathsLv();
        }

	    private bool IsPathValid(string path)
	    {
	        return !String.IsNullOrEmpty(path) && Directory.Exists(path);
	    }


        private void PropagateButton_Click(object sender, EventArgs e)
		{
			AppendPaths(GetSelectedProjects(), GetSelectedPaths());
			RefreshPathsLv();
		}
		private void PropagateAllButton_Click(object sender, EventArgs e)
		{
			AppendPaths(paths.Keys, GetSelectedPaths());
			RefreshPathsLv();
		}
		private IEnumerable<string> GetSelectedPaths()
		{
			return PathsLv.Items.Cast<ListViewItem>().Where(s => s.Selected).Select(s => s.Text);
		}
		private IEnumerable<Project> GetSelectedProjects()
		{
			return ProjectsLv.Items.Cast<ListViewItem>().Where(s => s.Selected).Select(s => (Project)s.Tag);
		}		
		private void DeletePaths(IEnumerable<Project> projects, IEnumerable<string> pathList)
		{
			foreach (var path in pathList)
			{
				DeletePath(projects, path);
			}
		}
		private void DeletePath(IEnumerable<Project> projects, string path)
		{
			foreach (var proj in projects)
			{
				var list = paths[proj];
				for(var i = list.Count - 1; i >= 0; i--)
				{
					if (string.Equals(list[i], path, StringComparison.OrdinalIgnoreCase))
						list.RemoveAt(i);
				}
			}
		}
		private void AppendPaths(IEnumerable<Project> projects, IEnumerable<string> pathList)
		{
			foreach (var path in pathList)
			{
				AppendPath(projects, path);
			}
		}
		private void AppendPath(IEnumerable<Project> projects, string path)
		{
			foreach (var proj in projects)
			{
				var list = paths[proj];
				if (!list.Contains(path, StringComparer.OrdinalIgnoreCase))
					list.Add(path);
			}
		}

        private static void ShowErrorMsgBox(string errorTxt)
        {
            MessageBox.Show(errorTxt,
                Resources.ErrorMsgBoxTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation);
        }

        private readonly Dictionary<Project, List<string>> paths = new Dictionary<Project, List<string>>();
		private readonly Dictionary<Project, Property> pathProps = new Dictionary<Project, Property>();
		private readonly DTE2 applicationObject;

	}
}
