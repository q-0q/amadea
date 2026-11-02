using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Code.Fsm.GravityFsm.PlayerFsm;
using Code.TriggerParams;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public partial class InventoryMenuFsm
{
    
    private PlayerInput _playerInput;
    private CanvasGroup _canvasGroup;
    private CanvasGroup _listCanvasGroup;
    private CanvasGroup _useConfirmationCanvasGroup;
    private Image _closeImage;

    public static InventoryMenuFsm Singleton;
    

    private GameObject _listSelection;
    private TextMeshProUGUI _listSelectionName;
    private TextMeshProUGUI _listSelectionDescription;
    private TextMeshProUGUI _listSelectionUseDescription;
    private int _listSelectionListItemId;
    
    
    private TextMeshProUGUI _useConfirmationItemName;
    private TextMeshProUGUI _useConfirmation;
    
    private InventoryListItem.InventoryListItemData _confirmationData;
    
    public Button navBagButton;
    public Button navMovelistButton;
    public Button navTestButton;
    public GameObject listContentParent;
    
    public GameObject useButton;

    private List<Button> navButtons;

    private bool _xMoveInput;
    public Image useInputImage;

    public static event Action<string> OnInventoryTabSelected; 

    public void OnQuitClicked()
    {
        Application.Quit();
    }

    public void OnNewGameConfirmClicked()
    {
        SceneLoader.Singleton.LoadScene(SaveSystem.LoadCachedSaveData().scene);
    }

    private bool NeedToSelect(GameObject currentObject)
    {
        foreach (var selectable in currentObject.GetComponentsInChildren<Selectable>())
        {
            if (EventSystem.current.currentSelectedGameObject == selectable.gameObject) return false;
        }

        return _playerInput.actions["Move"].ReadValue<Vector2>().magnitude > 0.01f;
    }

    private void PopulateBagData(SaveSystem.SaveData obj)
    {
        var data = new List<InventoryListItem.InventoryListItemData>();
        foreach (var item in obj.items)
        {
            var keyItemRegistration = KeyItemRegistry.KeyItemRegistrations[item];
            var useType = keyItemRegistration.GetCanUse() ? InventoryListItem.UseType.Use : InventoryListItem.UseType.None;
            var inputAction = "";
            if (item == "Map")
            {
                useType = InventoryListItem.UseType.Input;
                inputAction = "Map";
            }
            if (item == "Camera")
            {
                useType = InventoryListItem.UseType.Input;
                inputAction = "Camera";
            }

            data.Add(new InventoryListItem.InventoryListItemData()
            {
                description = keyItemRegistration.description,
                displayName = keyItemRegistration.displayName,
                id = item,
                subText = keyItemRegistration.GetUseDescription(),
                UseType = useType,
                confirmation = keyItemRegistration.GetUseConfirmation(),
                dividerText = "",
                inputAction = inputAction,
            });
        }

        PopulateListView(data);
    }
    
    private void PopulateMovelistData(SaveSystem.SaveData obj)
    {
        var data = new List<InventoryListItem.InventoryListItemData>();
        
        data.Add(new InventoryListItem.InventoryListItemData()
        {
            dividerText = "<color=#" + MovelistRegistry.TrickColor +  ">- Lotus Forms ------------</color>",
            UseType = InventoryListItem.UseType.None
        });
        
        foreach (var (item, registration) in MovelistRegistry.TrickMovelistRegistrations)
        {

            var requiredTrick = registration.requiredTrick;
            if (requiredTrick != "")
            {
                if (!SaveSystem.GetTrick(requiredTrick)) continue;
            }
            
            
            data.Add(new InventoryListItem.InventoryListItemData()
            {
                description = registration.description,
                displayName = registration.displayName,
                id = item,
                subText = registration.useClause,
                UseType = InventoryListItem.UseType.Input,
                dividerText = "",
                inputAction = registration.useInput,
            });
        }
        
        data.Add(new InventoryListItem.InventoryListItemData()
        {
            dividerText = "- Basic actions ------------",
            UseType = InventoryListItem.UseType.None
        });
        
        foreach (var (item, registration) in MovelistRegistry.BasicMovelistRegistrations)
        {
            data.Add(new InventoryListItem.InventoryListItemData()
            {
                description = registration.description,
                displayName = registration.displayName,
                id = item,
                subText = registration.useClause,
                UseType = InventoryListItem.UseType.Input,
                dividerText = "",
                inputAction = registration.useInput,
            });
        }

        PopulateListView(data);
    }

    private void OnGameMenuOpened()
    {
        if (Machine.IsInState(InventoryMenuFsmState.Closed)) return;
        _canvasGroup.alpha = 0;
        _canvasGroup.blocksRaycasts = false;
    }

    private void OnGameMenuClosed()
    {
        if (Machine.IsInState(InventoryMenuFsmState.Closed)) return;
        _canvasGroup.alpha = 1;
        _canvasGroup.blocksRaycasts = true;
    }

    private void PopulateListView(List<InventoryListItem.InventoryListItemData> data)
    {
        for (int i = listContentParent.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(listContentParent.transform.GetChild(i).gameObject);
        }
        var prefab = Resources.Load("Prefab/InventoryListItem") as GameObject;

        var dividerPrefab = Resources.Load("Prefab/InventoryListDivider") as GameObject;

        bool selected = false;
        var buttons = new List<Button>();
        for (int i = 0; i < data.Count; i++)
        {
            if (data[i].dividerText == "")
            {
                var obj = Instantiate(prefab, listContentParent.transform);
                obj.name = data[i].id;
                var button = obj.GetComponentInChildren<Button>();
                buttons.Add(button);
                obj.GetComponent<InventoryListItem>().SetItemData(data[i]);
                if (!selected)
                {
                    button.Select();
                    selected = true;
                }
            }
            else
            {
                var obj = Instantiate(dividerPrefab, listContentParent.transform);
                obj.GetComponentInChildren<TextMeshProUGUI>().text = data[i].dividerText;
            }
            
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            var prevId = i == 0 ? buttons.Count - 1 : i - 1;
            var nextId = i == buttons.Count - 1 ? 0 : i + 1;
            var navigation = buttons[i].navigation;
            navigation.selectOnUp = buttons[prevId];
            navigation.selectOnDown = buttons[nextId];
            buttons[i].navigation = navigation;
        }
    }
    
    private void OnListItemSelected(InventoryListItem.InventoryListItemData data) 
    {
        if (data == null)
        {
            _listSelection.SetActive(false);
            return;
        }
        _listSelection.SetActive(true);
        _listSelection.transform.DOComplete();
        _listSelection.transform.DOPunchRotation(Vector3.forward * 2f, 0.15f, 20, 1f);
        
        _listSelectionName.text = data.displayName;
        _listSelectionDescription.text = data.description;
        _listSelectionUseDescription.text = data.subText;

        useButton.gameObject.SetActive(data.UseType == InventoryListItem.UseType.Use);
        useInputImage.gameObject.SetActive(data.UseType == InventoryListItem.UseType.Input);
        useInputImage.sprite = InputTypeManager.Singleton.GetSpriteForAction(data.inputAction);
        _useConfirmationItemName.text = data.displayName;
        _useConfirmation.text = data.confirmation;
        _confirmationData = data;
        
        
    }

    private void OnInventoryTabSelectedMethod(string label)
    {
        if (label == "Bag") Machine.Fire(InventoryMenuFsmTrigger.Bag);
        if (label == "Movelist") Machine.Fire(InventoryMenuFsmTrigger.Movelist);
    }

    public void OnUseConfirmation()
    {
        KeyItemRegistry.KeyItemRegistrations[_confirmationData.id].onUse();
        Machine.Fire(InventoryMenuFsmTrigger.Use);
    }

    private IEnumerator DelayedSelect(Selectable selectable)
    {
        yield return null;
        selectable.Select();
    }

    private void OnPhotoModeActivated()
    {
        _canvasGroup.alpha = 0;
        if (Machine.IsInState(InventoryMenuFsmState.Open)) PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.IdleLong);
        Machine.Jump(InventoryMenuFsmState.Closed);
    }

    private void OnPhotoModeDeactivated()
    {
        
    }
}