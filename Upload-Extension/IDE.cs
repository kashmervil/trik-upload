using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace Trik.Upload_Extension
{
    internal class IDE
    {
        public IDE(SynchronizationContext context, IVsStatusbar statusbar, IVsOutputWindowPane pane)
        {
            Statusbar = new StatusbarImpl(context, statusbar);
            WindowPane = new WindowPaneImpl(context, pane);
        }

        public WindowPaneImpl WindowPane;

        public StatusbarImpl Statusbar;

        public List<string> GetSolutionProjects(IEnumerable solution)
        {
            var list = new List<string>();
            foreach (var project in solution.OfType<Project>())
            {
                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                    list.AddRange(GetSolutionFolderProjects(project));
                else
                    list.Add(project.FullName);
            }
            return list.Where(x => !String.IsNullOrWhiteSpace(x)).ToList();
            //{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC} -- C#
            //{f2a71f9b-5d33-465a-a702-920d77279786} -- F#
        }

        private List<string> GetSolutionFolderProjects(Project projectFolder)
        {
            var list = new List<string>();
            foreach (var project in projectFolder.ProjectItems.OfType<ProjectItem>()
                .Select(item => item.SubProject)
                .Where(project => project != null))
            {
                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                    list.AddRange(GetSolutionFolderProjects(project));
                else
                {
                    list.Add(project.FullName);
                }
            }
            return list;
        }

    }
}