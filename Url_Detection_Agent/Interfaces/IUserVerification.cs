namespace Url_Detection_Agent.Utils
{
    public interface IUserVerification
    {
        string ShowDialog(string prompt, string title, string defaultValue = null, int? xPos = null, int? yPos = null);
        bool IsLocalLicenseValid();
    }
}