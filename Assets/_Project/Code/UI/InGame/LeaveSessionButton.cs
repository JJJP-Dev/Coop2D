using UnityEngine;
using UnityEngine.UI;

public class LeaveSessionButton : MonoBehaviour
{
    private Button _leaveBtn;

    private void Awake()
    {
        _leaveBtn = GetComponent<Button>();
        _leaveBtn.onClick.AddListener(OnClickLeaveBtn);
    }

    private void OnClickLeaveBtn()
    {
        ConnectionService.Instance.LeaveSession();
    }
}
