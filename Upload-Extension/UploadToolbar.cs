using System.ComponentModel.Design;

namespace UploadExtension
{
    internal class UploadToolbar
    {
        //public MenuCommand Connect { get; set; }
        public MenuCommand Upload { get; set; }
        public MenuCommand RunProgram { get; set; }
        public MenuCommand StopProgram { get; set; }
        public MenuCommand Properties { get; set; }
        public string DropDownListMessage { get; set; }
        public string OptionsMessage { get; set; }
    }
}