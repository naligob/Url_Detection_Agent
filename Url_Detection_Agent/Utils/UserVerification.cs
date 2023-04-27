using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Configuration;
using System.Reflection;
using Url_Detection_Agent.Services;
using static Url_Detection_Agent.Enum.Enums;

namespace Url_Detection_Agent.Utils
{
    public class UserVerification : IUserVerification
    {
        private readonly IConfiguration _configuration;
        private readonly IAPIService _APIService;
        #region Interface
        public UserVerification(IConfiguration configuration, IAPIService aPIService)
        {
            _configuration = configuration;
            _APIService = aPIService;
        }
        public bool IsLocalLicenseValid()
        {
            var result = false;
            var localLicence = _configuration["ClientLicence"];
            if (!string.IsNullOrEmpty(localLicence))
                result = _APIService.CheckLicense(localLicence).Is_valid;

            return result;
        }
        public UserVerificationStatus ShowDialog(string prompt, string title, string defaultValue = null, int? xPos = null, int? yPos = null)
        {
            InputBoxDialog form = new InputBoxDialog(prompt, title, _configuration, _APIService, defaultValue, xPos, yPos);
            DialogResult result = form.ShowDialog();
            if (result == DialogResult.Cancel)
                return UserVerificationStatus.Fail;
            else
                return UserVerificationStatus.Success;
        }
        #endregion

        #region Auxiliary class
        private class InputBoxDialog : Form
        {
            public string Value { get { return _txtInput.Text; } }

            private Label _lblPrompt;
            private TextBox _txtInput;
            private Button _btnOk;
            private Button _btnCancel;
            private readonly IConfiguration _configuration;
            private readonly IAPIService _APIService;
            private readonly ErrorProvider _errorProvider;

            #region Constructor
            public InputBoxDialog(string prompt, string title, IConfiguration configuration,IAPIService aPIService, string defaultValue = null, int? xPos = null, int? yPos = null)
            {
                _configuration = configuration;
                _APIService = aPIService;

                _errorProvider = new ErrorProvider();

                if (xPos == null && yPos == null)
                {
                    StartPosition = FormStartPosition.CenterParent;
                }
                else
                {
                    StartPosition = FormStartPosition.Manual;

                    if (xPos == null) xPos = (Screen.PrimaryScreen.WorkingArea.Width - Width) >> 1;
                    if (yPos == null) yPos = (Screen.PrimaryScreen.WorkingArea.Height - Height) >> 1;

                    Location = new Point(xPos.Value, yPos.Value);
                }

                InitializeComponent();

                if (title == null) title = Application.ProductName;
                Text = title;

                _lblPrompt.Text = prompt;
                Graphics graphics = CreateGraphics();
                _lblPrompt.Size = graphics.MeasureString(prompt + "y", _lblPrompt.Font).ToSize();

                int promptWidth = _lblPrompt.Size.Width * 5;
                int promptHeight = _lblPrompt.Size.Height * 4;

                _txtInput.Location = new Point(12, promptHeight);
                int inputWidth = promptWidth < 206 ? 206 : promptWidth;
                _txtInput.Size = new Size(inputWidth - 30, 21);
                _txtInput.Text = defaultValue;
                _txtInput.SelectAll();
                _txtInput.Focus();

                Height = 125 + promptHeight;
                Width = inputWidth + 23;
                Icon = new Icon(
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                    @"Images\security_icon.ico"));

                _btnOk.Location = new Point(8, 60 + promptHeight);
                _btnOk.Size = new Size(100, 22);

                _btnCancel.Location = new Point(promptWidth - 100, 60 + promptHeight);
                _btnCancel.Size = new Size(100, 22);
                _btnCancel.CausesValidation = false;
            }

            #endregion

            #region Methods
            protected void InitializeComponent()
            {
                _lblPrompt = new Label();
                _lblPrompt.Location = new Point(12, 9);
                _lblPrompt.TabIndex = 0;
                _lblPrompt.BackColor = Color.Transparent;

                _txtInput = new TextBox();
                _txtInput.Size = new Size(156, 20);
                _txtInput.TabIndex = 1;
                _txtInput.Validating += _txtInput_Validating;

                _btnOk = new Button();
                _btnOk.TabIndex = 2;
                _btnOk.Size = new Size(75, 26);
                _btnOk.Text = "&OK";
                _btnOk.DialogResult = DialogResult.OK;


                _btnCancel = new Button();
                _btnCancel.TabIndex = 3;
                _btnCancel.Size = new Size(75, 26);
                _btnCancel.Text = "&Cancel";
                _btnCancel.DialogResult = DialogResult.Cancel;

                AcceptButton = _btnOk;
                CancelButton = _btnCancel;

                Controls.Add(_lblPrompt);
                Controls.Add(_txtInput);
                Controls.Add(_btnOk);
                Controls.Add(_btnCancel);

                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;

            }

            private void _txtInput_Validating(object? sender, System.ComponentModel.CancelEventArgs e)
            {
                TextBox tb = sender as TextBox ?? new TextBox();
                var keyInput = tb.Text;

                if (string.IsNullOrEmpty(keyInput))
                {
                    _errorProvider.SetError(tb, "*");
                    e.Cancel = true;
                    return;
                }
                var clientLicence = _configuration["ClientLicence"];
                if (clientLicence == null)
                {
                    MessageBox.Show("Error in installed files, please reinstall the program");
                    return;
                }
                var serverResponse = _APIService.CheckLicense(keyInput);
                if (serverResponse.statusCode != 0)
                {
                    MessageBox.Show("Error in server communication, please try again later");
                    return;
                }
                if (!serverResponse.Is_valid)
                {
                    MessageBox.Show("License is invalid");
                    _errorProvider.SetError(tb, "*");
                    e.Cancel = true;
                }
                else
                {
                    UpdateAppSettings("ClientLicence", keyInput);

                    e.Cancel = false;
                }

            }

            private static void UpdateAppSettings<T>(string key, T value)
            {
                try
                {
                    var filePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory,
                        $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json");
                    var json = File.ReadAllText(filePath);
                    dynamic jsonObj = JsonConvert.DeserializeObject(json);

                    if (!string.IsNullOrEmpty(key))
                    {
                        jsonObj[key] = value;
                    }
                    var output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);

                    File.WriteAllText(filePath, output);
                }
                catch (ConfigurationErrorsException)
                {
                    Console.WriteLine("Error rewriting app settings");
                }
            }
            #endregion
        }
        #endregion
    }
}
