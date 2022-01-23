using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;
using System.Collections;

namespace ExampleMod
{
    public class PerfectCloneItem : PassiveItem
    {

        //Call this method from the Start() method of your ETGModule extension
        public static void Register()
        {
            //The name of the item
            string itemName = "Perfect Clone";

            //Refers to an embedded png in the project. Make sure to embed your resources! Google it
            string resourceName = "PerfectClone/Resources/perfect_clone_sprite.png";

            //Create new GameObject
            GameObject obj = new GameObject(itemName);

            //Add a PassiveItem component to the object
            var item = obj.AddComponent<PerfectCloneItem>();

            //Adds a sprite component to the object and adds your texture to the item sprite collection
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj);

            //Ammonomicon entry variables
            string shortDesc = "Me, But Better";
            string longDesc = "Perfect Inmortality. \n\n" + 
				"Through decades of genetics research, humanity invented perfect cloning techniques. \n" +
				"However, it was never used outside of testing due to its ethical complications. \n\n" +
				"Ethics aren't really a thing in the Gungeon, though...";

            //Adds the item to the gungeon item list, the ammonomicon, the loot table, etc.
            //Do this after ItemBuilder.AddSpriteToObject!
            ItemBuilder.SetupItem(item, shortDesc, longDesc, "dino");

            //Set the rarity of the item
            item.quality = PickupObject.ItemQuality.S;
        }

        public override void Pickup(PlayerController player)
        {
			player.healthHaver.OnPreDeath += this.HandlePreDeath;
			base.Pickup(player);
        }

        public override DebrisObject Drop(PlayerController player)
        {
			player.healthHaver.OnPreDeath -= this.HandlePreDeath;
            return base.Drop(player);
        }

		private void HandlePreDeath(Vector2 damageDirection)
		{
			if (this.m_owner)
			{
				if (this.m_owner.IsInMinecart)
				{
					this.m_owner.currentMineCart.EvacuateSpecificPlayer(this.m_owner, true);
				}
				for (int i = 0; i < this.m_owner.passiveItems.Count; i++)
				{
					if (this.m_owner.passiveItems[i] is CompanionItem && this.m_owner.passiveItems[i].DisplayName == "Pig")
					{
						return;
					}
					if (this.m_owner.passiveItems[i] is ExtraLifeItem)
					{
						ExtraLifeItem extraLifeItem = this.m_owner.passiveItems[i] as ExtraLifeItem;
						if (extraLifeItem.extraLifeMode == ExtraLifeItem.ExtraLifeMode.DARK_SOULS)
						{
							return;
						}
					}
				}
			}
			if (this.m_owner.IsInMinecart)
			{
				this.m_owner.currentMineCart.EvacuateSpecificPlayer(this.m_owner, true);
			}

			HandlePerfectCloneItem(this.m_owner);
			
		}

		private void HandlePerfectCloneItem(PlayerController player)
        {
			if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
			{
				PlayerController otherPlayer = GameManager.Instance.GetOtherPlayer(player);
				if (otherPlayer.IsGhost)
				{
					base.StartCoroutine(HandlePerfectCloneEffect(player));
				}
				else
				{
					player.m_cloneWaitingForCoopDeath = true;
				}
			}
			else
			{
				base.StartCoroutine(HandlePerfectCloneEffect(player));
			}
		}

        private IEnumerator HandlePerfectCloneEffect(PlayerController player)
        {
			Pixelator.Instance.FadeToBlack(0.5f, false, 0f);
			GameUIRoot.Instance.ToggleUICamera(false);
			player.healthHaver.FullHeal();
			player.IsOnFire = false;
			player.CurrentFireMeterValue = 0f;
			player.CurrentPoisonMeterValue = 0f;
			player.CurrentCurseMeterValue = 0f;
			player.CurrentDrainMeterValue = 0f;
			if (player.characterIdentity == PlayableCharacters.Robot)
			{
				player.healthHaver.Armor = 6f;
			}
			float ela = 0f;
			while (ela < 0.5f)
			{
				ela += GameManager.INVARIANT_DELTA_TIME;
				yield return null;
			}
			int targetLevelIndex = 1;
			if (GameManager.Instance.CurrentGameMode == GameManager.GameMode.SHORTCUT)
			{
				targetLevelIndex += GameManager.Instance.LastShortcutFloorLoaded;
			}
			GameManager.Instance.SetNextLevelIndex(targetLevelIndex);
			if (GameManager.Instance.CurrentGameMode == GameManager.GameMode.BOSSRUSH)
			{
				GameManager.Instance.DelayedLoadBossrushFloor(0.5f);
			}
			else
			{
				GameManager.Instance.DelayedLoadNextLevel(0.5f);
			}
			player.m_cloneWaitingForCoopDeath = false;
			ExtraLifeItem cloneItem = null;
			for (int i = 0; i < player.passiveItems.Count; i++)
			{
				if (player.passiveItems[i] is ExtraLifeItem)
				{
					ExtraLifeItem extraLifeItem = player.passiveItems[i] as ExtraLifeItem;
					if (extraLifeItem.extraLifeMode == ExtraLifeItem.ExtraLifeMode.CLONE)
					{
						cloneItem = extraLifeItem;
					}
				}
			}
			/*if (cloneItem != null) // it's all copy pasting except for this part. this is literally the only thing that makes perfect clone different from normal clone
			{
				player.RemovePassiveItem(cloneItem.PickupObjectId);
			}*/
			if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
			{
				for (int j = 0; j < GameManager.Instance.AllPlayers.Length; j++)
				{
					PlayerController playerController = GameManager.Instance.AllPlayers[j];
					if (playerController.IsGhost)
					{
						playerController.StartCoroutine(playerController.CoopResurrectInternal(playerController.transform.position, null, true));
					}
					playerController.healthHaver.FullHeal();
					playerController.specRigidbody.Velocity = Vector2.zero;
					playerController.knockbackDoer.TriggerTemporaryKnockbackInvulnerability(1f);
					if (playerController.m_returnTeleporter != null)
					{
						playerController.m_returnTeleporter.ClearReturnActive();
						playerController.m_returnTeleporter = null;
					}
				}
				Chest.ToggleCoopChests(false);
			}
			else
			{
				player.healthHaver.FullHeal();
				player.specRigidbody.Velocity = Vector2.zero;
				player.knockbackDoer.TriggerTemporaryKnockbackInvulnerability(1f);
				if (player.m_returnTeleporter != null)
				{
					player.m_returnTeleporter.ClearReturnActive();
					player.m_returnTeleporter = null;
				}
			}
			yield return new WaitForSeconds(1f);
			player.IsOnFire = false;
			player.CurrentFireMeterValue = 0f;
			player.CurrentPoisonMeterValue = 0f;
			player.CurrentCurseMeterValue = 0f;
			player.CurrentDrainMeterValue = 0f;
			player.healthHaver.FullHeal();
			if (player.characterIdentity == PlayableCharacters.Robot)
			{
				player.healthHaver.Armor = 6f;
			}
			yield break;
		}
	}
}