using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using ReferencePathManager.Properties;
using VSLangProj;

namespace ReferencePathManager
{
    public partial class SolutionReferencePathsForm : Form
    {
        public SolutionReferencePathsForm(DTE2 applicationObject)
        {
            manager = ProjectReferencePathsManager.Load(applicationObject.Solution);
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var items = manager.ProjectInfos.Select(s => new ListViewItem(s.Name) { Tag = s });
            ProjectsLv.Items.AddRange(items.ToArray());
        }

        private void ProjectsLv_ItemSelectionChanged(object sender, EventArgs e) => RefreshPathsLv();
        private void AcceptBtn_Click(object sender, EventArgs e)
        {
            manager.SavePaths();
        }
        private void DeleteButton_Click(object sender, EventArgs e)
        {
            manager.RemovePaths(GetSelectedPaths(), GetSelectedProjectInfos());
            RefreshPathsLv();
        }
        private void DeleteAllButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Resources.DeletePathsFromAllProjects, "", MessageBoxButtons.OKCancel) != DialogResult.OK)
                return;
            manager.RemovePaths(GetSelectedPaths());
            RefreshPathsLv();
        }
        private void AddButton_Click(object sender, EventArgs e)
        {
            if (FolderBrowserDialog.ShowDialog() != DialogResult.OK)
                return;
            manager.AddPath(FolderBrowserDialog.SelectedPath, GetSelectedProjectInfos());
            RefreshPathsLv();
        }
        private void PropagateButton_Click(object sender, EventArgs e)
        {
            manager.AddPaths(GetSelectedPaths(), GetSelectedProjectInfos());
            RefreshPathsLv();
        }
        private void PropagateAllButton_Click(object sender, EventArgs e)
        {
            manager.AddPaths(GetSelectedPaths());
            RefreshPathsLv();
        }

        private void RefreshPathsLv()
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var selected = GetSelectedProjectInfos().ToList();
            foreach (var path in selected.SelectMany(s => s.Paths))
            {
                dict.TryGetValue(path, out var count);
                dict[path] = ++count;
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
        private IEnumerable<string> GetSelectedPaths()
        {
            return PathsLv.Items.Cast<ListViewItem>().Where(s => s.Selected).Select(s => s.Text);
        }
        private IEnumerable<ProjectInfo> GetSelectedProjectInfos()
        {
            return ProjectsLv.Items.Cast<ListViewItem>().Where(s => s.Selected).Select(s => (ProjectInfo)s.Tag);
        }

        private readonly ProjectReferencePathsManager manager;
    }

    internal class ProjectReferencePathsManager
    {
        private ProjectReferencePathsManager(IEnumerable<ProjectInfo> projectInfos)
        {
            foreach (var projectInfo in projectInfos)
            {
                var value = (string)projectInfo.ReferencePathProperty.Value;
                if (!string.IsNullOrEmpty(value))
                    projectInfo.Paths.AddRange(value.Split(';'));
                projectInfoMap[projectInfo.Project] = projectInfo;
            }
        }

        public static ProjectReferencePathsManager Load(Solution sln)
        {
            var infos = GetAllProjectInfos(sln).Where(s => s.ReferencePathProperty != null);
            return new ProjectReferencePathsManager(infos);
        }
        private static IEnumerable<ProjectInfo> GetAllProjectInfos(Solution sln)
        {
            return sln.Projects.Cast<Project>().SelectMany(s => GetProjectInfos(s, ""));
        }
        private static IEnumerable<ProjectInfo> GetProjectInfos(Project project, string folderPath)
        {
            if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
            {
                folderPath += project.Name + "/";
                return project.ProjectItems.Cast<ProjectItem>()
                    .Select(s => s.SubProject)
                    .Where(s => s != null)
                    .SelectMany(s => GetProjectInfos(s, folderPath));
            }
            return new[] { new ProjectInfo(project, folderPath + project.Name) };
        }

        public void SavePaths()
        {
            foreach (var proj in projectInfoMap.Values)
            {
                proj.ReferencePathProperty.Value = string.Join(";", proj.Paths.ToArray());
            }
        }
        public void RemovePaths(IEnumerable<string> pathList, IEnumerable<ProjectInfo> projectInfos = null)
        {
            foreach (var path in pathList)
            {
                // ReSharper disable once PossibleMultipleEnumeration
                RemovePath(path, projectInfos);
            }
        }
        public void RemovePath(string path, IEnumerable<ProjectInfo> projectInfos = null)
        {
            foreach (var list in (projectInfos ?? projectInfoMap.Values).Select(s => s.Paths))
            {
                list.RemoveAll(s => string.Equals(path, s, StringComparison.OrdinalIgnoreCase));
            }
        }
        public void AddPaths(IEnumerable<string> pathList, IEnumerable<ProjectInfo> projectInfos = null)
        {
            foreach (var path in pathList)
            {
                // ReSharper disable once PossibleMultipleEnumeration
                AddPath(path, projectInfos);
            }
        }
        public void AddPath(string path, IEnumerable<ProjectInfo> projectInfos = null)
        {
            var pathLists = (projectInfos ?? projectInfoMap.Values).Select(s => s.Paths)
                .Where(s => !s.Contains(path, StringComparer.OrdinalIgnoreCase));
            foreach (var list in pathLists)
            {
                list.Add(path);
            }
        }

        public IEnumerable<ProjectInfo> ProjectInfos => projectInfoMap.Values;

        private readonly Dictionary<Project, ProjectInfo> projectInfoMap = new Dictionary<Project, ProjectInfo>();
    }

    internal class ProjectInfo
    {
        public ProjectInfo(Project project, string name)
        {
            Project = project ?? throw new ArgumentNullException(nameof(project));
            Name = name;
            var props = project.Properties;
            if (props != null && supportedProjectKinds.Contains(project.Kind))
                ReferencePathProperty = props.Cast<Property>().FirstOrDefault(s => s.Name == "ReferencePath");
        }

        public Project Project { get; }
        public string Name { get; }
        public List<string> Paths { get; } = new List<string>();
        public Property ReferencePathProperty { get; }

        private static readonly string[] supportedProjectKinds = { PrjKind.prjKindCSharpProject, PrjKind.prjKindVBProject };
    }
}
