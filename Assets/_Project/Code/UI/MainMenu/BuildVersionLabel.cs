using TMPro;
using UnityEngine;

// Shows the game's version, so testers can tell which build they have open.
// It comes from Project Settings > Player > Version, the same one the host checks (#24).
[RequireComponent(typeof(TMP_Text))]
public class BuildVersionLabel : MonoBehaviour
{
    [Tooltip("{0} is the version. Development builds add Build Type after it.")]
    [SerializeField] private string _format = "v{0}";

    [Tooltip("What development builds add after the version: dev, internal... Release builds add nothing.")]
    [SerializeField] private string _buildType = "dev";

    private void Awake()
    {
        string version = Application.version;

        if (Debug.isDebugBuild)
            version += " " + _buildType;

        GetComponent<TMP_Text>().text = string.Format(_format, version);
    }
}
