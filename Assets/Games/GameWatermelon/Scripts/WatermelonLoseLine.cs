using UnityEngine;

namespace AiMiniGames.GameWatermelon
{
    // 挂在失败线物体上，负责把水果进入/离开失败区域的状态通知给控制器。
    [RequireComponent(typeof(Collider2D))]
    public sealed class WatermelonLoseLine : MonoBehaviour
    {
        [SerializeField] private WatermelonGameController controller;

        private void Reset()
        {
            ConfigureCollider();
        }

        private void Awake()
        {
            ConfigureCollider();
        }

        private void OnValidate()
        {
            ConfigureCollider();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryNotifyFruitEntered(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryNotifyFruitEntered(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (controller == null)
            {
                return;
            }

            if (other.TryGetComponent<WatermelonFruit>(out var fruit))
            {
                controller.NotifyFruitExitedLoseLine(fruit);
            }
        }

        private void ConfigureCollider()
        {
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        private void TryNotifyFruitEntered(Collider2D other)
        {
            if (controller == null)
            {
                return;
            }

            if (other.TryGetComponent<WatermelonFruit>(out var fruit))
            {
                controller.NotifyFruitEnteredLoseLine(fruit);
            }
        }
    }
}
