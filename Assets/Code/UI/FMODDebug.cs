using TMPro;
using UnityEngine;

public class FMODDebug : MonoBehaviour
{
    private TextMeshProUGUI _tmp;

    private void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        FMODUnity.RuntimeManager.CoreSystem.getChannelsPlaying(out int totalChannels, out int realChannels);
        _tmp.text = "TCs: " + totalChannels + " RCs: " + realChannels;
    }
}
