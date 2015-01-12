using UnityEngine;
using UnityEngine.UI;

public class Tree : MonoBehaviour
{

    void Start()
    {
        var image = GetComponentInChildren<Image>();
        var r = Random.Range(.5f, 1f);
        var g = Random.Range(.5f, 1f);
        var b = Random.Range(.5f, 1f);
        image.color = new Color(r, g, b);

        var dx = Random.Range(-2, 3);
        var dy = Random.Range(-2, 3);
        transform.Translate(dx, dy, 0, Space.Self);
    }

}
