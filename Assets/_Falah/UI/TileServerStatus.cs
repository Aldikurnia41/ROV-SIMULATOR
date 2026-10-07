using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Falah.RovSim.UI
{
    /// <summary>Dot + label showing whether the local tile server answers (HEAD request, 3 s timeout).</summary>
    public sealed class TileServerStatus : MonoBehaviour
    {
        [SerializeField] string url = "http://192.168.101.39:8080/kalimantan/tileset.json";
        [SerializeField] Image dot;
        [SerializeField] TMP_Text label;

        void OnEnable() => StartCoroutine(Check());

        IEnumerator Check()
        {
            Apply(UiTheme.Warn, "Memeriksa server tile lokal...");
            using (var request = UnityWebRequest.Head(url))
            {
                request.timeout = 3;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success) Apply(UiTheme.Ok, "Server tile lokal terhubung");
                else Apply(UiTheme.Danger, "Server tile lokal tidak terjangkau");
            }
        }

        void Apply(Color color, string text)
        {
            if (dot != null) dot.color = color;
            if (label != null) label.text = text;
        }
    }
}
