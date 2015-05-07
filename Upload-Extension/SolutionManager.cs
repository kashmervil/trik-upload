using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trik.Upload_Extension
{
    internal class SolutionManager
    {
        private readonly string _libconwrapPath = Path.GetDirectoryName(typeof (Uploader).Assembly.Location) +
                                                  @"\Resources\libconWrap.so.1.0.0";

        private readonly Uploader _uploader;

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
                try
                {
                    if (ActiveProject == null)
                        throw new InvalidOperationException(
                            "Calling UploadActiveProjectAsync before setting ActiveProject property");

                    if (ActiveProject.UploadedFiles.Count == 0)
                    {
                        ActiveProject.Initialize();
                        //Or reinitialize (e.g You have changed something in your project's configuration
                        //and want these changes to take place in remote machine.
                        //Make clean of your project -> upload -> build -> upload 
                        var command = String.Format("mkdir {0}; ln /home/root/trik-sharp/uploads/{1} {0}{1}; {2}",
                            ActiveProject.FilesUploadPath, Path.GetFileName(_libconwrapPath), ActiveProject.Script);
                        _uploader.ExecuteCommand(command);
                    }

                    var newFiles =
                        Directory.GetFiles(ActiveProject.ProjectLocalBuildPath).Where(s => !(s.Contains(".vshost")
                                                                                             || s.EndsWith(".config")
                                                                                             || s.EndsWith(".pdb")));
                    var uploadedFiles = ActiveProject.UploadedFiles;
                    var filesToRemove = uploadedFiles.Keys.Where(s => !newFiles.Contains(s)).ToArray();
                    _uploader.RemoveFiles(filesToRemove, ActiveProject.FilesUploadPath);
                    Array.ForEach(filesToRemove, x => ActiveProject.UploadedFiles.Remove(x));


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

        public void RunProgram()
        {
            _uploader.SendCommandToStream(". " + ActiveProject.RemoteScriptName);
        }

        public void StopProgram()
        {
            _uploader.SendCommandToStream(Encoding.ASCII.GetString(new byte[]{3})); // Ctrl+C code
        }
    }
}