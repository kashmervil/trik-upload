using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Trik.Upload_Extension
{
    internal class SolutionManager
    {
        private readonly Uploader _uploader;
        private readonly string _libconwrapPath = Path.GetDirectoryName(typeof (Uploader).Assembly.Location) + @"\Resources\libconWrap.so.1.0.0";

        public SolutionManager(string name, Uploader uploader)
        {
            FullName = name;
            _uploader = uploader;
            Projects = new List<UploadProjectInfo>();
        }

        public SolutionManager(string name, IList<string> projects, Uploader uploader)
        {
            FullName = name;
            _uploader = uploader;
            Projects = new List<UploadProjectInfo>();
            foreach (var project in projects)
            {
                Projects.Add(new UploadProjectInfo(project));
            }
        }

        public List<UploadProjectInfo> Projects { get; private set; }
        public string FullName { get; private set; }
        public UploadProjectInfo ActiveProject { get; set; }

        public void UpdateProjects(IList<string> newProjects)
        {
            var oldProjects = Projects.Select(x => x.ProjectFilePath).ToList();
            foreach (var i in newProjects.Except(oldProjects))
            {
                Projects.Add(new UploadProjectInfo(i));
            }
            foreach (var i in oldProjects.Except(newProjects))
            {
                Projects.RemoveAll(x => x.ProjectFilePath == i);
            }
        }
        public async Task<string> UploadActiveProjectAsync()
        {
            return await Task.Run(() => //TODO: Remove this ***
            {
                var error = "";
                if (ActiveProject == null)
                    throw new InvalidOperationException(
                        "Calling UploadActiveProjectAsync before setting ActiveProject property");
                try
                {
                    if (ActiveProject.UploadedFiles.Count == 0)
                    {
                        _uploader.ExecuteCommand("mkdir " + ActiveProject.FilesUploadPath + "; " + ActiveProject.Script);
                        _uploader.UploadFile(new FileInfo(_libconwrapPath),
                            ActiveProject.FilesUploadPath + Path.GetFileName(_libconwrapPath));
                    }

                    var newFiles = Directory.GetFiles(ActiveProject.ProjectLocalBuildPath);
                    var uploadedFiles = ActiveProject.UploadedFiles;

                    foreach (var file in newFiles)
                    {
                        if (!uploadedFiles.ContainsKey(file))
                        {
                            uploadedFiles.Add(file, DateTime.MinValue);
                        }
                        var info = new FileInfo(file);
                        if (info.LastWriteTime <= uploadedFiles[file]) continue;
                        _uploader.UploadFile(info, ActiveProject.FilesUploadPath + Path.GetFileName(file));
                        uploadedFiles[file] = info.LastWriteTime;
                    }
                }
                catch (Exception exception)
                {
                    error = exception.Message;
                }
                return error;
            });
        }
        public void RunProgram(){}

        public void StopProgram()
        {
            _uploader.ExecuteCommand("killall mono");
        }
    }
}