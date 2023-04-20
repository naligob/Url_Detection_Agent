using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using Xceed.Wpf.Toolkit;

namespace Url_Detection_Agent
{
    [RunInstaller(true)]
    public partial class DetectorInstaller : Installer
    {
        public DetectorInstaller()
        {
            InitializeComponent();
        }
        public override void Install(IDictionary stateSaver)
        {
            var key = Context.Parameters["EDITA1"];
            if (File.Exists(@"C:\Users\nalig\OneDrive - Bar-Ilan University\Desktop\Agent\Agent_Setup\Data.txt"))
                File.Delete(@"C:\Users\nalig\OneDrive - Bar-Ilan University\Desktop\Agent\Agent_Setup\Data.txt");
            File.WriteAllText(@"C:\Users\nalig\OneDrive - Bar-Ilan University\Desktop\Agent\Agent_Setup\Data.txt",
                $"key={key}");
            //Console.WriteLine($"ori haze {key}");
            base.Install(stateSaver);
        }
        public override void Commit(IDictionary savedState)
        {
            var key = Context.Parameters["EDITA1"];
            if (File.Exists(@"C:\Users\nalig\OneDrive - Bar-Ilan University\Desktop\Agent\Agent_Setup\Data.txt"))
                File.Delete(@"C:\Users\nalig\OneDrive - Bar-Ilan University\Desktop\Agent\Agent_Setup\Data.txt");
            File.WriteAllText(@"C:\Users\nalig\OneDrive - Bar-Ilan University\Desktop\Agent\Agent_Setup\Data.txt",
                $"key={key}");
            base.Commit(savedState);
        }
        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
        }
        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);
        }
    }
}
