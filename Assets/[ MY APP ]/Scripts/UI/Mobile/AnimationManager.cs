using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    static AnimationManager m_instance;
    public static AnimationManager Instance { get => m_instance; }

    UIManager uiManager;
    MobileManager mobileManager;

    private void Awake()
    {
        m_instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        uiManager = UIManager.Instance;
        mobileManager = MobileManager.Instance;
    }

    //Tab và TabButton hiện tại sẽ chạy animation close và mở tab và tabutton được chọn lên.
    public void PlayTabAnimation(int buttonIndex)
    {
        for (int j = 0; j < mobileManager.TabButtons.Count; j++)
        {
            //Lấy animator của j.
            Animator _tabButtonActivateAnimator = mobileManager.TabButtons[j].GetComponent<Animator>();
            Animator _tabActivateAnimator = mobileManager.Tabs[j].GetComponent<Animator>();

            //Xét tab j đang mở
            if (_tabActivateAnimator.gameObject.activeInHierarchy)
            {
                //isOpen là parameters được tạo trong animator.
                //Đóng TabButton hiện tại.
                if (_tabButtonActivateAnimator.GetBool("isOpen"))
                    _tabButtonActivateAnimator.SetBool("isOpen", false);

                //Đóng Tab hiện tại.
                if (!mobileManager.TabButtons[buttonIndex].GetComponent<Animator>().GetBool("isOpen"))
                    mobileManager.TabButtons[buttonIndex].GetComponent<Animator>().SetBool("isOpen", true);

                //Nếu j không bằng buttonIndex có nghĩa là không nhấn vào tabButton hiện tại. Nên sẽ chạy animation mở tab mới và đóng tab hiện tại.
                if (buttonIndex != j)
                    StartCoroutine(CloseAnimationEvent(_tabActivateAnimator, "Tab Panel Close", mobileManager.Tabs[buttonIndex]));
            }
        }
    }

    //Chạy animation đóng cho gameobject hiện tại và sau khi chạy aniamtion xong mới chạy ActivateUI cho gameobject cần mở.
    public IEnumerator CloseAnimationEvent(Animator animator, string stateName, GameObject activateObject)
    {
        float animationLength = GetAnimationStateLength(animator, stateName);
        animator.GetComponent<Animator>().Play(stateName);

        if (animationLength > 0)
        {
            yield return new WaitForSeconds(animationLength);

            uiManager.ActivateUI(activateObject);
        }
        else
        {
            Debug.Log("Animation state '" + stateName + "' not found.");
        }
    }

    public void CloseAnnounce(Animator animator)
    {
        StartCoroutine(IE_CloseDeleteFileAnnounce(animator));
    }

    //Dùng để mở bảng thông báo của delete hay passcode. Chức năng tương tụ CloseAnimationEvent nhưng sẽ không có mở một gameobject khác mà chỉ chạy aniamtion xong và tắt chính nó.
    IEnumerator IE_CloseDeleteFileAnnounce(Animator animator)
    {
        float animationLength = GetAnimationStateLength(animator, "Fade-out");
        animator.GetComponent<Animator>().Play("Fade-out");

        if (animationLength > 0)
        {
            yield return new WaitForSeconds(animationLength);

            animator.gameObject.SetActive(false);
        }
    }

    //Lấy độ dài của animation.
    float GetAnimationStateLength(Animator animator, string stateName)
    {
        // Access the AnimatorController's AnimationClips
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;

        if (controller != null)
        {
            // Check each AnimationClip's name to find the one that matches the state name
            foreach (AnimationClip clip in controller.animationClips)
            {
                if (clip.name == stateName)
                {
                    return clip.length;
                }
            }
        }

        // Return -1 if the animation state was not found
        return -1;
    }
}
