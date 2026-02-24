using PurrNet;
using UnityEngine;
public class EquippableItem : NetworkBehaviour
{
    [Header("Item Base Info")]
    public string itemName;
    public bool isEquipped;

    public virtual void OnEquip()
    {
        isEquipped = true;
        gameObject.SetActive(true);
    }

    public virtual void OnUnequip()
    {
        isEquipped = false;
        gameObject.SetActive(false);
    }
    public virtual void UseItem(bool inputDown, bool inputHeld, bool inputUp)
    {
    }
}