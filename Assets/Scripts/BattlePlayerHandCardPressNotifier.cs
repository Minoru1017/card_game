using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 我方手牌根節點：按下／放開／滑出時通知 <see cref="BattleSimulationDebugUI"/>，用於暫時隱藏「你的回合」浮窗，並在放開後銜接「觸碰手牌未出牌」閒置計時。
/// </summary>
public class BattlePlayerHandCardPressNotifier : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private BattleSimulationDebugUI _host;
    private bool _pressing;

    public void Init(BattleSimulationDebugUI host)
    {
        _host = host;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_host == null) return;
        _pressing = true;
        _host.NotifyPlayerHandCardPressBegan();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EndPressIfNeeded();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 按住拖離手牌區時可能收不到 Up 在同一張牌上，仍以離開視為結束操作。
        EndPressIfNeeded();
    }

    private void EndPressIfNeeded()
    {
        if (!_pressing || _host == null) return;
        _pressing = false;
        _host.NotifyPlayerHandCardPressEnded();
    }
}
