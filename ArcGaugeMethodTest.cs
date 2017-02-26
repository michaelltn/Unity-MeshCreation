using UnityEngine;
using System.Collections;

public class ArcGaugeMethodTest : MonoBehaviour
{
    public ArcGauge arcGauge;

    void OnGUI()
    {
        if (arcGauge != null)
        {
            Rect buttonRect = new Rect(10, 10, 150, 25);

            if (GUI.Button(buttonRect, "addSection(true)"))
            {
                arcGauge.addSection(true);
            }

            buttonRect.y += (buttonRect.height + 10);

            if (GUI.Button(buttonRect, "addSection(false)"))
            {
                arcGauge.addSection(false);
            }

            buttonRect.y += (buttonRect.height + 10);

            if (GUI.Button(buttonRect, "removeSection(true)"))
            {
                arcGauge.removeSection(true);
            }

            buttonRect.y += (buttonRect.height + 10);

            if (GUI.Button(buttonRect, "removeSection(false)"))
            {
                arcGauge.removeSection(false);
            }

        }
    }
}
