using UnityEngine;

public static class PlayerPrefsResetter
{
    private const string VersionKey = "SavedAppVersion";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CheckAndResetPrefs()
    {
        string currentVersion = Application.version;
        string savedVersion = PlayerPrefs.GetString(VersionKey, string.Empty);

        if (savedVersion != currentVersion)
        {
            // Clear all existing preferences
            PlayerPrefs.DeleteAll();

            // Set the new version and write immediately to disk
            PlayerPrefs.SetString(VersionKey, currentVersion);
            PlayerPrefs.Save();

            Debug.Log($"[PlayerPrefs] Wiped old preferences. Updated to version {currentVersion}");
        }
    }
}