using UnityEngine;

namespace Data.Scripts
{
    [CreateAssetMenu(menuName = "Sliding Data",fileName = "slidingData")]
    public class SlidingData : ScriptableObject
    {
        [field: SerializeField] public float MaxSlideTime { get; private set; }
        [field: SerializeField] public float SlideForce { get; private set; }
        [field: SerializeField] public float SlideYScale { get; private set; }
    }
}