using UnityEngine;

public abstract class PowerUp : MonoBehaviour
{
    public abstract string Name { get; }
    public abstract string Description { get; }

    protected abstract void PickUp();

    protected abstract void Use();
}
