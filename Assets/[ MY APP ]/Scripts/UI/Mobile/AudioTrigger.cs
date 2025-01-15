
using UnityEngine.EventSystems;
using UnityEngine;

public class AudioTrigger : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    AudioManager m_audioManager;

    private void Start()
    {
        m_audioManager = AudioManager.Instance;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        m_audioManager.PlaySound(m_audioManager.HoverSound);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        m_audioManager.PlaySound(m_audioManager.ClickSound);
    }
}