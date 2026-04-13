using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool Interact;
		public bool Inventory;
		public bool QuickSlot1;
		public bool QuickSlot2;
		public bool QuickSlot3;
		public bool QuickSlot4;
		public bool QuickSlot5;
		public bool QuickSlot6;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}

		public void OnInteract(InputValue value)
		{
			InteractInput(value.isPressed);
		}

		public void OnInventory(InputValue value)
		{
			InventoryInput(value.isPressed);
		}

		public void OnQuickSlot1(InputValue value)
		{
			QuickSlot1 = value.isPressed;
		}

		public void OnQuickSlot2(InputValue value)
		{
			QuickSlot2 = value.isPressed;
		}

		public void OnQuickSlot3(InputValue value)
		{
			QuickSlot3 = value.isPressed;
		}	

		public void OnQuickSlot4(InputValue value)
		{
			QuickSlot4 = value.isPressed;
		}

		public void OnQuickSlot5(InputValue value)
		{
			QuickSlot5 = value.isPressed;
		}

		public void OnQuickSlot6(InputValue value)
		{
			QuickSlot6 = value.isPressed;
		}
#endif
		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		public void InteractInput(bool newInteractState)
		{
			Interact = newInteractState;
		}

		public void InventoryInput(bool newInventoryState)
		{
			Inventory = newInventoryState;
		}

		public void OnQuickSlot1Input(bool newQuickSlot1State)
		{
			QuickSlot1 = newQuickSlot1State;
		}

		public void OnQuickSlot2Input(bool newQuickSlot2State)
		{
			QuickSlot2 = newQuickSlot2State;
		}

		public void OnQuickSlot3Input(bool newQuickSlot3State)
		{
			QuickSlot3 = newQuickSlot3State;
		}

		public void OnQuickSlot4Input(bool newQuickSlot4State)
		{
			QuickSlot4 = newQuickSlot4State;
		}

		public void OnQuickSlot5Input(bool newQuickSlot5State)
		{
			QuickSlot5 = newQuickSlot5State;
		}

		public void OnQuickSlot6Input(bool newQuickSlot6State)
		{
			QuickSlot6 = newQuickSlot6State;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}