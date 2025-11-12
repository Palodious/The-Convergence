using UnityEngine;

public class PlayerSpecialization : MonoBehaviour
{
    public Element[] elements;
    public int currentElementIndex;

    public void CycleElement()
    {
        currentElementIndex = (currentElementIndex + 1) % elements.Length;
        Debug.Log("Switched to: " + GetCurrentElement().elementName);
    }

    public Element GetCurrentElement()
    {
        return elements[currentElementIndex];
    }
}

public class Element
{
    [SerializeField] public string elementName;
    [SerializeField] public string elementType; // "Fire", "Snow", "Lightning", "Crystal", "Plasma"
    [SerializeField] public Color color;
    [Range(0.5f, 2f)]
    [SerializeField] public float areaScale = 1f;
    [SerializeField] public int damageBonus;
}