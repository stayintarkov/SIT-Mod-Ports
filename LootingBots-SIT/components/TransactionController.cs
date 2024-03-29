using System;
using System.Linq;
using System.Threading.Tasks;

using Comfort.Common;

using EFT;
using EFT.InventoryLogic;
using EFT.UI;

using JetBrains.Annotations;

using LootingBots.Patch.Util;

using InventoryControllerResultStruct = SOperationResult;
using InventoryHelperClass = ItemMovementHandler;
using GridClassEx = GridContainer;
using GridCacheClass = GClass1398;

namespace LootingBots.Patch.Components
{
    public class TransactionController
    {
        readonly BotLog _log;
        readonly InventoryControllerClass _inventoryController;
        readonly BotOwner _botOwner;
        public bool Enabled;

        public TransactionController(
            BotOwner botOwner,
            InventoryControllerClass inventoryController,
            BotLog log
        )
        {
            _botOwner = botOwner;
            _inventoryController = inventoryController;
            _log = log;
        }

        public class EquipAction
        {
            public SwapAction Swap;
            public MoveAction Move;
        }

        public class SwapAction
        {
            public Item ToThrow;
            public Item ToEquip;
            public ActionCallback Callback;
            public ActionCallback OnComplete;

            public SwapAction(
                Item toThrow = null,
                Item toEquip = null,
                ActionCallback callback = null,
                ActionCallback onComplete = null
            )
            {
                ToThrow = toThrow;
                ToEquip = toEquip;
                Callback = callback;
                OnComplete = onComplete;
            }
        }

        public class MoveAction
        {
            public Item ToMove;
            public ItemAddress Place;
            public ActionCallback Callback;
            public ActionCallback OnComplete;

            public MoveAction(
                Item toMove = null,
                ItemAddress place = null,
                ActionCallback callback = null,
                ActionCallback onComplete = null
            )
            {
                ToMove = toMove;
                Place = place;
                Callback = callback;
                OnComplete = onComplete;
            }
        }

        public delegate Task ActionCallback();

        /** Tries to add extra spare ammo for the weapon being looted into the bot's secure container so that the bots are able to refill their mags properly in their reload logic */
        public bool AddExtraAmmo(Weapon weapon)
        {
            try
            {
                SearchableItemClass secureContainer = (SearchableItemClass)
                    _inventoryController.Inventory.Equipment
                        .GetSlot(EquipmentSlot.SecuredContainer)
                        .ContainedItem;

                // Try to get the current ammo used by the weapon by checking the contents of the magazine. If its empty, try to create an instance of the ammo using the Weapon's CurrentAmmoTemplate
                Item ammoToAdd =
                    weapon.GetCurrentMagazine()?.FirstRealAmmo()
                    ?? Singleton<ItemFactory>.Instance.CreateItem(
                        new MongoID(false),
                        weapon.CurrentAmmoTemplate._id,
                        null
                    );

                // Check to see if there already is ammo that meets the weapon's caliber in the secure container
                bool alreadyHasAmmo =
                    secureContainer
                        .GetAllItems()
                        .Where(
                            item =>
                                item is BulletClass bullet
                                && bullet.Caliber.Equals(((BulletClass)ammoToAdd).Caliber)
                        )
                        .ToArray()
                        .Length > 0;

                // If we dont have any ammo, attempt to add 10 max ammo stacks into the bot's secure container for use in the bot's internal reloading code
                if (!alreadyHasAmmo)
                {
                    _log.LogDebug($"Trying to add ammo");
                    int ammoAdded = 0;

                    for (int i = 0; i < 10; i++)
                    {
                        Item ammo = ammoToAdd.CloneItem();
                        ammo.StackObjectsCount = ammo.StackMaxSize;

                        string[] visitorIds = new string[] { _inventoryController.ID };

                        var location = _inventoryController.FindGridToPickUp(
                            ammo,
                            secureContainer.Grids
                        );

                        if (location != null)
                        {
                            var result = location.AddWithoutRestrictions(ammo, visitorIds);
                            if (result.Failed)
                            {
                                _log.LogError(
                                    $"Failed to add {ammo.Name.Localized()} to secure container"
                                );
                            }
                            else
                            {
                                ammoAdded += ammo.StackObjectsCount;
                                Singleton<GridCacheClass>.Instance.Add(
                                    location.GetOwner().ID,
                                    location.Grid as GridClassEx,
                                    ammo
                                );
                            }
                        }
                        else
                        {
                            _log.LogError(
                                $"Cannot find location in secure container for {ammo.Name.Localized()}"
                            );
                        }
                    }

                    if (ammoAdded > 0)
                    {
                        _log.LogDebug(
                            $"Successfully added {ammoAdded} round of {ammoToAdd.Name.Localized()}"
                        );
                    }
                }
                else
                {
                    _log.LogDebug($"Already has ammo for {weapon.Name.Localized()}");
                }

                return true;
            }
            catch (Exception e)
            {
                _log.LogError(e);
            }

            return false;
        }

        /** Tries to find an open Slot to equip the current item to. If a slot is found, issue a move action to equip the item */
        public async Task<bool> TryEquipItem(Item item)
        {
            try
            {
                // Check to see if we can equip the item
                var ableToEquip = _inventoryController.FindSlotToPickUp(item);
                if (ableToEquip != null)
                {
                    _log.LogWarning(
                        $"Equipping: {item.Name.Localized()} [place: {ableToEquip.Container.ID.Localized()}]"
                    );
                    bool success = await MoveItem(new MoveAction(item, ableToEquip));
                    return success;
                }

                _log.LogDebug($"Cannot equip: {item.Name.Localized()}");
            }
            catch (Exception e)
            {
                _log.LogError(e);
            }

            return false;
        }

        /** Tries to find a valid grid for the item being looted. Checks all containers currently equipped to the bot. If there is a valid grid to place the item inside of, issue a move action to pick up the item */
        public async Task<bool> TryPickupItem(Item item)
        {
            try
            {
                var ableToPickUp = _inventoryController.FindGridToPickUp(item);

                if (
                    ableToPickUp != null
                    && !ableToPickUp
                        .GetRootItem()
                        .Parent.Container.ID.ToLower()
                        .Equals("securedcontainer")
                )
                {
                    _log.LogWarning(
                        $"Picking up: {item.Name.Localized()} [place: {ableToPickUp.GetRootItem().Name.Localized()}]"
                    );
                    return await MoveItem(new MoveAction(item, ableToPickUp));
                }

                _log.LogDebug($"No valid slot found for: {item.Name.Localized()}");
            }
            catch (Exception e)
            {
                _log.LogError(e);
            }
            return false;
        }
        /*
        // TraderControllerClass.Class2110.method_0
        public void method_0_2(IResult result)
        {
            Callback callback = callback2;
            if (callback == null)
            {
                return;
            }
            callback.Invoke(result);
        }
        
        // TraderControllerClass.Class2110.operation
        public AbstractInventoryOperation operation2;
        
        // TraderControllerClass.Class2110.callback
        public Callback callback2;
        
        // TraderControllerClass.Execute
        public virtual void Execute(AbstractInventoryOperation operation, [CanBeNull] Callback callback)
        {
            operation2 = operation;
            callback2 = callback;
            if (_inventoryController.vmethod_0(operation2))
            {
                operation2.vmethod_0(new Callback(method_0_2), false);
                return;
            }
            operation2.Dispose();
            Callback callback3 = callback2;
            if (callback3 == null)
            {
                return;
            }
            CallbackExtensions.Fail(callback3, string.Format("Can't execute {0}", operation2), 1);
        }
        
        // TraderControllerClass.CanExecute
        public bool CanExecute(IOperationResult operationResult)
        {
            return operationResult != null && operationResult.CanExecute(_inventoryController);
        }
        
        // TraderControllerClass RunNetworkTransaction
        public void RunNetTrans(IOperationResult operationResult, Callback callback = null)
        {
            if (!CanExecute(operationResult) && callback != null)
            {
                CallbackExtensions.Fail(callback, "Execution discarded locally");
            }
            AbstractInventoryOperation abstractInventoryOperation = _inventoryController.ConvertOperationResultToOperation(operationResult);
            Execute(abstractInventoryOperation, callback);
        }
        
        // TraderControllerClass.Class2109.method_0
        public void method_0_1(IResult result)
        {
            callbackTask1.SetResult(result);
            Callback callback = callback1;
            if (callback == null)
            {
                return;
            }
            callback.Invoke(result);
        }
        
        // TraderControllerClass.Class2109.callbackTask
        public TaskCompletionSource<IResult> callbackTask1;
        
        // TraderControllerClass.Class2109.callback
        public Callback callback1;
        
        // TraderControllerClass TryRunNetworkTransaction
        public virtual Task<IResult> TryRunNetTrans(SOperationResult operationResult, Callback callback = null)
        {
            callback1 = callback;
            callbackTask1 = new TaskCompletionSource<IResult>();
            if (operationResult.Failed)
            {
                AsyncExtensions.Fail(callbackTask1, operationResult.Error.ToString(), 0);
            }
            else if (operationResult.Value.CanExecute(_inventoryController))
            {
                RunNetTrans(operationResult.Value, new Callback(method_0_1));
            }
            else
            {
                AsyncExtensions.Fail(callbackTask1, "Can not execute", 0);
                Callback callback2 = callback1;
                if (callback2 != null)
                {
                    callback2.Invoke(new FailedResult("Can't execute 'operationResult.Value.CanExecute()'", 0));
                }
            }
            return callbackTask1.Task;
        }
        */

        /** Moves an item to a specified item address. Supports executing a callback */
        public async Task<bool> MoveItem(MoveAction moveAction)
        {
            try
            {
                if (IsLootingInterrupted())
                {
                    return false;
                }

                if (moveAction.ToMove is Weapon weapon && !(moveAction.ToMove is BulletClass))
                {
                    AddExtraAmmo(weapon);
                }

                _log.LogDebug($"Moving item to: {moveAction?.Place?.Container?.ID?.Localized()}");
                var value = InventoryHelperClass.Move(
                    moveAction.ToMove,
                    moveAction.Place,
                    _inventoryController,
                    true
                );

                if (value.Failed)
                {
                    _log.LogError(
                        $"Failed to move {moveAction.ToMove.Name.Localized()} to {moveAction.Place.Container.ID.Localized()}"
                    );
                    return false;
                }

                if (moveAction.Callback == null)
                {
                    await SimulatePlayerDelay();
                     await _inventoryController.TryRunNetworkTransaction(value, null);
                    //await TryRunNetTrans(value, null);
                }

                else
                {
                    TaskCompletionSource<IResult> promise = new TaskCompletionSource<IResult>();

                    //await TryRunNetTrans(
                    await TryRunNetworkTransaction(
                        value,
                        new Callback(
                            async (IResult result) =>
                            {
                                if (result.Succeed)
                                {
                                    await SimulatePlayerDelay();
                                    await moveAction.Callback();
                                }
                                promise.TrySetResult(result);
                            }
                        )
                    );
                    
                    await promise.Task;
                }
                if (moveAction.OnComplete != null)
                {
                    await SimulatePlayerDelay();
                    await moveAction.OnComplete();
                }
            }
            catch (Exception e)
            {
                _log.LogError(e);
            }

            return true;
        }

        /** Method used when we want the bot the throw an item and then equip an item immidiately afterwards */
        public async Task<bool> ThrowAndEquip(SwapAction swapAction)
        {
            if (IsLootingInterrupted())
            {
                return false;
            }

            try
            {
                TaskCompletionSource<IResult> promise = new TaskCompletionSource<IResult>();
                Item toThrow = swapAction.ToThrow;

                _log.LogWarning($"Throwing item: {toThrow.Name.Localized()}");
                _inventoryController.ThrowItem(
                    toThrow,
                    null,
                    new Callback(
                        async (IResult result) =>
                        {
                            if (result.Succeed && swapAction.Callback != null)
                            {
                                await SimulatePlayerDelay();
                                await swapAction.Callback();
                            }

                            promise.TrySetResult(result);
                        }
                    ),
                    false
                );
                await SimulatePlayerDelay();
                IResult taskResult = await promise.Task;
                if (taskResult.Failed)
                {
                    return false;
                }

                if (swapAction.OnComplete != null)
                {
                    await swapAction.OnComplete();
                }

                return true;
            }
            catch (Exception e)
            {
                _log.LogError(e);
            }

            return false;
        }

        public Task<IResult> TryRunNetworkTransaction(
            InventoryControllerResultStruct operationResult,
            Callback callback = null
        )
        {
            return _inventoryController.TryRunNetworkTransaction(operationResult, callback);
        }

        public bool IsLootingInterrupted()
        {
            return !Enabled;
        }

        public static Task SimulatePlayerDelay(float delay = -1f)
        {
            if (delay == -1)
            {
                delay = LootingBots.TransactionDelay.Value;
            }

            return Task.Delay(TimeSpan.FromMilliseconds(delay));
        }
    }
}
