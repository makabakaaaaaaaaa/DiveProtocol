using System.Collections;
using DiveProtocol.Builds;
using DiveProtocol.Interaction;
using DiveProtocol.Loot;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiveProtocol.Tests.PlayMode
{
    public sealed class RogueliteDropPickupPlayModeTests
    {
        [UnityTest]
        public IEnumerator HealingAndAmmoDropsUseTheExistingPlayerApis()
        {
            var player = new GameObject("Drop Pickup Player");
            var healingDrop = new GameObject("Healing Drop");
            var ammoDrop = new GameObject("Ammo Drop");
            try
            {
                HealthComponent health = player.AddComponent<HealthComponent>();
                PlayerHitscanWeapon weapon = player.AddComponent<PlayerHitscanWeapon>();
                yield return null;

                health.TakeDamage(new DamageInfo(30f, null, Vector3.zero, Vector3.zero));
                weapon.SetAmmo(10);

                RogueliteDropPickup healing = healingDrop.AddComponent<RogueliteDropPickup>();
                healing.Configure(DropItemType.HealingDrop, 15);
                RogueliteDropPickup ammo = ammoDrop.AddComponent<RogueliteDropPickup>();
                ammo.Configure(DropItemType.AmmoDrop, 5);

                Assert.That(healing.TryCollect(player), Is.True);
                Assert.That(health.CurrentHealth, Is.EqualTo(85f).Within(0.001f));
                Assert.That(ammo.TryCollect(player), Is.True);
                Assert.That(weapon.CurrentAmmo, Is.EqualTo(15));
            }
            finally
            {
                Object.Destroy(player);
                Object.Destroy(healingDrop);
                Object.Destroy(ammoDrop);
            }
        }

        [UnityTest]
        public IEnumerator DropRequiresInteractionAndDoesNotCollectOnAvailability()
        {
            var player = new GameObject("Manual Drop Player");
            var dropObject = new GameObject("Manual Healing Drop");
            try
            {
                HealthComponent health = player.AddComponent<HealthComponent>();
                player.AddComponent<PlayerHitscanWeapon>();
                player.AddComponent<PlayerBuildController>();
                PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
                health.TakeDamage(new DamageInfo(30f, null, Vector3.zero, Vector3.zero));

                BoxCollider trigger = dropObject.AddComponent<BoxCollider>();
                RogueliteDropPickup drop = dropObject.AddComponent<RogueliteDropPickup>();
                drop.Configure(DropItemType.HealingDrop, 15);
                yield return new WaitForFixedUpdate();

                Assert.That(trigger.isTrigger, Is.True);
                Assert.That(dropObject.layer, Is.EqualTo(LayerMask.NameToLayer("Interactable")));
                Assert.That(health.CurrentHealth, Is.EqualTo(70f).Within(0.001f));
                Assert.That(drop.CanInteract(player), Is.True);
                Assert.That(drop.UsesScreenPrompt, Is.True);
                Assert.That(drop.InteractionPrompt, Is.EqualTo("Pick up Medkit"));

                yield return null;

                Assert.That(interactor.CurrentInteractable, Is.SameAs(drop));
                drop.Interact(player);
                yield return null;
                Assert.That(health.CurrentHealth, Is.EqualTo(85f).Within(0.001f));
            }
            finally
            {
                Object.Destroy(player);
                Object.Destroy(dropObject);
            }
        }
    }
}
