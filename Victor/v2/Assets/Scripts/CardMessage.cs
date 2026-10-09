using UnityEngine;

// Put this on each clickable card. It just stores the explanation text.
public class CardMessage : MonoBehaviour
{
    [TextArea(4, 12)]
    public string message;
}
