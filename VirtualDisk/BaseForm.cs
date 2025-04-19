using VirtualDisk.FileSystem;
using VirtualDisk.Client;
using VirtualDisk.Utils;

namespace VirtualDisk
{
    public partial class BaseForm : Form
    {
        public string Outputs { get; set; } = string.Empty;

        private void DoSync()
        {
            Storage.Write(App.Instance, "app.json");

            Module<WooZoooFS>.Instance.SetCookies(App.Instance.Cookies);
            Module<WooZoooFS>.Instance.Synchronize();
        }

        public BaseForm()
        {
            InitializeComponent();
        }

        private void BaseForm_Load(object sender, EventArgs e)
        {
            App.Instance = Storage.Read<App>("app.json");

            cookieEdit.DataBindings.Add("Text", App.Instance, "Cookies");

            Module<VirtualFS>.Instance.SetClient(Module<WooZoooFS>.Instance);
            DoSync();
            Module<VirtualFS>.Instance.Start();
        }

        private void Icon_DoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            Activate();
            WindowState = FormWindowState.Normal;
        }

        private void BaseForm_Closing(object sender, FormClosingEventArgs e)
        {

            if (e.CloseReason == CloseReason.ApplicationExitCall)
            {
                icon.Visible = false;
                Application.Exit();
            }
            else
            {
                e.Cancel = true;
                Hide();
                WindowState = FormWindowState.Minimized;
            }
        }

        private void Icon_Clicked(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                menu.Show(Cursor.Position);
            }
        }

        private void Menu_Clicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string? Name = e.ClickedItem?.Name;

            if (Name == "menuExit")
            {
                Application.Exit();
            }

            if (Name == "menuUpdate")
            {
                DoSync();
            }
        }

        private void Button_Save(object sender, EventArgs e)
        {
        }

        public void OnWrite(string? value)
        {
            if (value != null)
            {
                Outputs += value;
            }
        }

        private void Button_Sync(object sender, EventArgs e)
        {
            DoSync();
        }
    }
}
