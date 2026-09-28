using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Nevergreen.Data;

namespace Nevergreen.UI
{
    [RequireComponent(typeof(Image))]
    public class TrinketUIItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public TrinketData TrinketData { get; private set; }
        public PartyMemberInfo Owner { get; private set; }
        public int SlotIndex { get; private set; }
        public bool isShopContext { get; set; } = false;
        public ShopUIController shopController { get; set; } = null;

        private Image _image;
        private Transform _originalParent;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void Initialize(TrinketData data, PartyMemberInfo owner = null, int slotIndex = -1)
        {
            TrinketData = data;
            Owner = owner;
            SlotIndex = slotIndex;
            
            if (_image != null && data != null && data.illustration != null)
            {
                _image.sprite = data.illustration;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalParent = transform.parent;
            
            // Move to top-most canvas to ensure it renders above everything else during drag
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null)
            {
                transform.SetParent(rootCanvas.transform);
                transform.SetAsLastSibling();
            }

            _canvasGroup.blocksRaycasts = false; // Allow drops to pass through this item
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (this == null || gameObject == null) return;
            
            _canvasGroup.blocksRaycasts = true;

            // If we are not dropped onto a valid handler that changed our parent, revert to original parent
            if (transform.parent == GetComponentInParent<Canvas>()?.transform || transform.parent == null)
            {
                transform.SetParent(_originalParent);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            TrinketUIItem draggedItem = eventData.pointerDrag?.GetComponent<TrinketUIItem>();
            if (draggedItem != null && draggedItem != this)
            {
                // If draggedItem is from somewhere else, and this item is equipped to a character...
                if (Owner != null)
                {
                    PartyMemberInfo ownerA = draggedItem.Owner;
                    PartyMemberInfo ownerB = this.Owner;
                    TrinketData x = draggedItem.TrinketData;
                    TrinketData y = this.TrinketData;
                    
                    if (ownerA == null)
                    {
                        // From loot/inventory to equipped slot
                        if (y.cannotBeRemoved) return; // Rollback
                        
                        // Check duplicates
                        bool bHasX = false; 
                        foreach(var t in ownerB.equippedTrinkets) { if (t != y && t.trinketId == x.trinketId) bHasX = true; }
                        if (bHasX) return;

                        // Shop Affordability Check
                        ShopUIController currentShop = this.isShopContext ? this.shopController : null;
                        if (currentShop != null && !currentShop.CanAfford(draggedItem.SlotIndex)) return;

                        ownerB.TryUnequipTrinket(y);
                        if (ownerB.TryEquipTrinket(x))
                        {
                            if (currentShop != null)
                            {
                                currentShop.OnItemPurchased(draggedItem.SlotIndex);
                            }

                            if (!isShopContext) SaveManager.SaveRun();
                            
                            var controller = GetComponentInParent<PartyManagementPanelController>();
                            if (controller != null)
                            {
                                controller.ForceRefresh();
                            }

                            var treasureController = GetComponentInParent<TreasureUIController>();
                            if (treasureController != null)
                            {
                                treasureController.RefreshPanels();
                            }

                            if (currentShop != null)
                            {
                                currentShop.RefreshEquipPanelsPublic();
                            }
                            
                            // Reinitialize the dragged item (which goes back to inventory) 
                            // to act as the unequipped item 'y'.
                            draggedItem.Initialize(y, null, -1);
                        }
                        else
                        {
                            ownerB.TryEquipTrinket(y); // Rollback
                        }
                    }
                    else if (ownerA == ownerB)
                    {
                        // Swap position on same character
                        int indexA = ownerA.equippedTrinkets.IndexOf(x);
                        int indexB = ownerB.equippedTrinkets.IndexOf(y);
                        if (indexA != -1 && indexB != -1)
                        {
                            ownerA.equippedTrinkets[indexA] = y;
                            ownerB.equippedTrinkets[indexB] = x;
                            
                            if (!isShopContext) SaveManager.SaveRun();
                            
                            var controller = GetComponentInParent<PartyManagementPanelController>();
                            if (controller != null)
                            {
                                controller.ForceRefresh();
                            }

                            var treasureController = GetComponentInParent<TreasureUIController>();
                            if (treasureController != null)
                            {
                                treasureController.RefreshPanels();
                            }
                            
                            Destroy(draggedItem.gameObject);
                        }
                    }
                    else
                    {
                        // Swap between two characters
                        if (x.cannotBeRemoved || y.cannotBeRemoved) return;
                        
                        // Check duplicates
                        bool bHasX = false; 
                        foreach(var t in ownerB.equippedTrinkets) { if (t != y && t.trinketId == x.trinketId) bHasX = true; }
                        
                        bool aHasY = false;
                        foreach(var t in ownerA.equippedTrinkets) { if (t != x && t.trinketId == y.trinketId) aHasY = true; }
                        
                        if (bHasX || aHasY) return; // Rollback due to duplicate
                        
                        ownerA.TryUnequipTrinket(x);
                        ownerB.TryUnequipTrinket(y);
                        ownerA.TryEquipTrinket(y);
                        ownerB.TryEquipTrinket(x);
                        
                        if (!isShopContext) SaveManager.SaveRun();
                        
                        var controller = GetComponentInParent<PartyManagementPanelController>();
                        if (controller != null)
                        {
                            controller.ForceRefresh();
                        }

                        var treasureController2 = GetComponentInParent<TreasureUIController>();
                        if (treasureController2 != null)
                        {
                            treasureController2.RefreshPanels();
                        }
                        
                        Destroy(draggedItem.gameObject);
                    }
                }
            }
        }
    }
}
