using UnityEngine;
using UnityEngine.EventSystems;

public class MenuFirstSelected : MonoBehaviour
{
    [SerializeField] private GameObject firstSelected;

    void OnEnable()
    {
        if (EventSystem.current == null || firstSelected == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }
}