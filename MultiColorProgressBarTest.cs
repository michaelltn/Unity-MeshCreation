using UnityEngine;
using System.Collections;

public class MultiColorProgressBarTest : MonoBehaviour {

    public MultiColorProgressBar multiColorProgressBar;

	// Update is called once per frame
	void OnGUI () {
        if (multiColorProgressBar == null) return;

        int y = 10;

        if (GUI.Button(new Rect(y, 10, 120, 40), "Add 5% red"))
        {
            multiColorProgressBar.addValue(0.05f, Color.red);
        }
        y += 130;
        if (GUI.Button(new Rect(y, 10, 120, 40), "Add 5% blue"))
        {
            multiColorProgressBar.addValue(0.05f, Color.blue);
        }
        y += 130;
        if (GUI.Button(new Rect(y, 10, 120, 40), "Add 5% green"))
        {
            multiColorProgressBar.addValue(0.05f, Color.green);
        }
        y += 130;
        if (GUI.Button(new Rect(y, 10, 120, 40), "reset"))
        {
            multiColorProgressBar.resetValue();
        }
	}
}
