using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedText : MonoBehaviour {
    public int LinesShown = 15;
    public float LineIncrementIntervalSeconds = 0.8f;

    private TextMesh textMesh;

    private string allText;
    private string[] allLines;
    private int currentLineViewIndex = 0;

	void Start () {
        textMesh = this.GetComponent<TextMesh>();
        allText = textMesh.text;
        allLines = allText.Split('\n');
        currentLineViewIndex = -LinesShown;
        StartCoroutine(Scroll());
	}

    private IEnumerator Scroll()
    {
        while (isActiveAndEnabled)
        {
            if (currentLineViewIndex >= allLines.Length)
                currentLineViewIndex = -LinesShown;
            RefreshText();
            currentLineViewIndex++;

            yield return new WaitForSeconds(LineIncrementIntervalSeconds);
        }
    }

    private void RefreshText()
    {
        string newText = "";
        for (int i = 0; i < LinesShown; i++)
        {
            int lineI = currentLineViewIndex + i;
            if (lineI > -1 && lineI < allLines.Length)
                newText += allLines[lineI] + '\n';
            else
                newText += "\r\n";
        }
        textMesh.text = newText;
    }
}
