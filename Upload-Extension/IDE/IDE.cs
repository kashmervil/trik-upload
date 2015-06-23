using System.Collections;
using System.Collections.Generic;

namespace UploadExtension.IDE
{
    internal interface IDE<TStatusBar, TWindowPane>
    {
        void Dispose();
        List<string> GetSolutionProjects(IEnumerable solution);
        TStatusBar Statusbar { get; set; }
        TWindowPane WindowPane { get; set; }
    }
}