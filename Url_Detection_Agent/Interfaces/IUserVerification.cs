using static Url_Detection_Agent.Enum.Enums;

namespace Url_Detection_Agent.Utils
{
    public interface IUserVerification
    {
        UserVerificationStatus ShowDialog(string prompt, string title, string defaultValue = null, int? xPos = null, int? yPos = null);
        bool IsLocalLicenseValid();
    }
}