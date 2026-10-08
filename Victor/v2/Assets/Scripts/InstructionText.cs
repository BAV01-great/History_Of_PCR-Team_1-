using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InstructionText : MonoBehaviour
{
    public string[] allTexts;
    public GameObject rootObj;
    public TextMeshProUGUI bodyText, btnText;
    private int index = 0;
    
    public void Next(){
        if(index < allTexts.Length){
            if(index == allTexts.Length -1){
                btnText.text = "CLOSE";
            } else btnText.text = "NEXT >>>";
            bodyText.text = allTexts[index];
            index ++;
        } else rootObj.SetActive(false);
    }
}
