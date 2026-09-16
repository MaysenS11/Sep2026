using UnityEngine;

namespace Dungeon.Props
{
    [SelectionBase]
    public class PillarProp : MonoBehaviour
    {
        [SerializeField] private Collider2D pillarCollider;

        private void Reset()
        {
            pillarCollider = GetComponent<Collider2D>();
            if (pillarCollider == null)
            {
                pillarCollider = gameObject.AddComponent<BoxCollider2D>();
            }
        }
    }
}
