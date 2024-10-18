﻿using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.BasicMessages;

using PlayerRoles;
using PlayerRoles.FirstPersonControl.Spawnpoints;
using PlayerRoles.FirstPersonControl;

using PluginAPI.Core;
using PluginAPI.Core.Items;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MEC;
using Respawning;
using UnityEngine;
using Interactables.Interobjects.DoorUtils;
using Mirror;

using HelpSense.API.Features.Pool;

namespace HelpSense.Helper
{
    public static class XHelper
    {
        public static System.Random Random = new(DateTime.Now.GetHashCode());
        public static HashSet<Player> PlayerList = new();
        public static HashSet<Player> SpecialPlayerList = new();

        public static Player GetRandomPlayer(RoleTypeId roleTypeId)
        {
            List<Player> players = new List<Player>();

            foreach (Player player in PlayerList)
            {
                if (player.Role == roleTypeId)
                {
                    players.Add(player);
                }
            }

            if (players.Any())
            {
                return players[Random.Next(0, players.Count() - 1)];
            }

            return null;
        }
        public static Player GetRandomPlayer(RoleTypeId roleTypeId , List<Player> playerList)
        {
            List<Player> players = new List<Player>();
            foreach (Player player in playerList)
            {
                if (player.Role == roleTypeId)
                {
                    players.Add(player);
                }
            }
            if (players.Any())
            {
                return players[Random.Next(0, players.Count - 1)];
            }

            return null;
        }
        public static Player GetRandomPlayer(List<Player> playerList)
        {
            if (playerList.Any())
            {
                return playerList[Random.Next(0, playerList.Count() - 1)];
            }

            return null;
        }
        public static Player GetRandomSpecialPlayer(RoleTypeId roleTypeId)
        {
            List<Player> players = new List<Player>();
            foreach (Player player in SpecialPlayerList)
            {
                if (player.Role == roleTypeId)
                {
                    players.Add(player);
                }
            }
            if (players.Any())
            {
                var randomPlayer = players[Random.Next(0, players.Count() - 1)];
                SpecialPlayerList.Remove(randomPlayer);
                return randomPlayer;
            }

            return null;
        }
        public static ItemType GetRandomItem()
        {
            var allItems = Enum.GetValues(typeof(ItemType)).ToArray<ItemType>();

            return allItems[Random.Next(0, allItems.Length - 1)];
        }

        public static void SpawnItem(ItemType typeid, Vector3 position, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                ItemPickup.Create(typeid, position, new Quaternion(0, 0, 0, 0)).Spawn();
            }
        }
        public static ItemPickup SpawnItem(ItemType typeid, Vector3 position)
        {
            var item = ItemPickup.Create(typeid, position, new Quaternion(0, 0, 0, 0));
            item.Spawn();
            return item;
        }

        public static void SetPlayerScale(this Player target, Vector3 scale)
        {
            GameObject go = target.GameObject;
            if (go.transform.localScale == scale)
                return;

            try
            {
                NetworkIdentity identity = target.ReferenceHub.networkIdentity;
                go.transform.localScale = scale;
                foreach (Player player in PlayerList)
                {
                    NetworkServer.SendSpawnMessage(identity, player.Connection);
                }
            }
            catch (Exception e)
            {
                Log.Info($"Set Scale error: {e}");
            }
        }
        public static void SetPlayerScale(this Player target, float scale) => SetPlayerScale(target, Vector3.one * scale);
        public static bool PlayerScaleIs(this Player target, float scale) => PlayerScaleIs(target, Vector3.one * scale);
        public static bool PlayerScaleIs(this Player target, Vector3 scale) => target.GameObject.transform.localScale == scale;

        public static void MessageTranslated(string message, string translation, bool isHeld = false, bool isNoisy = true, bool isSubtitles = true)
        {
            StringBuilder announcement = StringBuilderPool.Pool.Get();
            string[] cassies = message.Split('\n');
            string[] translations = translation.Split('\n');
            for (int i = 0; i < cassies.Length; i++)
                announcement.Append($"{translations[i].Replace(' ', ' ')}<size=0> {cassies[i]} </size><split>");

            RespawnEffectsController.PlayCassieAnnouncement(announcement.ToString(), isHeld, isNoisy, isSubtitles);
            StringBuilderPool.Pool.Return(announcement);
        }

        //防倒卖
        public static IEnumerator<float> AutoXBroadcast()
        {
            yield return Timing.WaitForSeconds(30f);

            while (true)
            {
                yield return Timing.WaitForSeconds(360f);
                if (Round.IsRoundEnded || !Round.IsRoundStarted)
                {
                    yield break;
                }
                Broadcast("<size=35><align=center><color=#F6511D>此服务器在运行X小左的插件，享受你的游戏时间~</color></align></size>", 6, global::Broadcast.BroadcastFlags.Normal);
            }
        }

        public static IEnumerator<float> AutoServerBroadcast()
        {
            yield return Timing.WaitForSeconds(10f);

            while (true)
            {
                if (Round.IsRoundEnded || !Round.IsRoundStarted)
                {
                    yield break;
                }

                Broadcast(Plugin.Instance.TranslateConfig.AutoServerMessageText, Plugin.Instance.Config.AutoServerMessageTimer, global::Broadcast.BroadcastFlags.Normal);
                yield return Timing.WaitForSeconds(Plugin.Instance.Config.AutoServerMessageTime * 60f);
            }
        }

        public static IEnumerator<float> SCP191CoroutineMethod(Player player)
        {
            int d = 5000;
            while (true)
            {
                if (player is null || !player.IsAlive || Round.IsRoundEnded)
                {
                    yield break;
                }

                player.ReceiveHint(Plugin.Instance.TranslateConfig.SCP191BatteryHintShow.Replace("%Battery%" , d.ToString()), 11);//Use compatibility adapter
                
                if (player.Room.Name is MapGeneration.RoomName.Hcz079)
                {
                    if (d <= 4000)
                        d += 1000;
                    else if (d <= 5000)
                        d = 5100;
                }

                d -= 100;

                if (d <= 0)
                    player.Kill(Plugin.Instance.TranslateConfig.SCP191BatteryDepletionDeathReason);

                yield return Timing.WaitForSeconds(10f);
            }
        }

        public static bool IsSpecialPlayer(this Player player)
        {
            return player.RoleName is "SCP-029" or "SCP-703" or "SCP-191" or "SCP-073" or "SCP-2936-1" || player.RoleName == Plugin.Instance.TranslateConfig.ChaosLeaderRoleName;
        }

        public static bool BreakDoor(DoorVariant doorBase, DoorDamageType type = DoorDamageType.ServerCommand)
        {
            if (doorBase is not IDamageableDoor damageableDoor || damageableDoor.IsDestroyed)
                return false;

            damageableDoor.ServerDamage(ushort.MaxValue, type);
            return true;
        }

        public static void ReloadWeapon(this Player player)
        {
            if (player.CurrentItem == null)
                return;

            if (player.CurrentItem is not Firearm firearm)
                return;

            firearm.AmmoManagerModule.ServerTryReload();
            player.Connection.Send(new RequestMessage(firearm.ItemSerial, RequestType.Reload));
        }

        public static bool IsAmmo(this ItemType item)
        {
            if (item != ItemType.Ammo9x19 && item != ItemType.Ammo12gauge && item != ItemType.Ammo44cal && item != ItemType.Ammo556x45)
            {
                return item == ItemType.Ammo762x39;
            }

            return true;
        }

        public static bool IsWeapon(this ItemType type, bool checkHID = true)
        {
            switch (type)
            {
                case ItemType.GunCOM15:
                case ItemType.GunE11SR:
                case ItemType.GunCrossvec:
                case ItemType.GunFSP9:
                case ItemType.GunLogicer:
                case ItemType.GunCOM18:
                case ItemType.GunRevolver:
                case ItemType.GunAK:
                case ItemType.GunShotgun:
                case ItemType.ParticleDisruptor:
                case ItemType.GunCom45:
                case ItemType.GunFRMG0:
                case ItemType.Jailbird:
                    return true;
                case ItemType.MicroHID:
                    if (checkHID)
                    {
                        return true;
                    }

                    break;
            }

            return false;
        }

        public static bool IsScp(this ItemType type)
        {
            return type is ItemType.SCP018 or ItemType.SCP500 or ItemType.SCP268 or ItemType.SCP207 or ItemType.SCP244a or ItemType.SCP244b or ItemType.SCP2176;
        }

        public static bool IsThrowable(this ItemType type)
        {
            return type is ItemType.SCP018 or ItemType.GrenadeHE or ItemType.GrenadeFlash or ItemType.SCP2176;
        }

        public static bool IsMedical(this ItemType type)
        {
            return type is ItemType.Painkillers or ItemType.Medkit or ItemType.SCP500 or ItemType.Adrenaline;
        }

        public static bool IsUtility(this ItemType type)
        {
            return type is ItemType.Flashlight or ItemType.Radio;
        }

        public static bool IsArmor(this ItemType type)
        {
            return type is ItemType.ArmorLight or ItemType.ArmorCombat or ItemType.ArmorHeavy;
        }

        public static bool IsKeycard(this ItemType type)
        {
            switch (type)
            {
                case ItemType.KeycardScientist:
                case ItemType.KeycardResearchCoordinator:
                case ItemType.KeycardZoneManager:
                case ItemType.KeycardGuard:
                case ItemType.KeycardMTFPrivate:
                case ItemType.KeycardContainmentEngineer:
                case ItemType.KeycardMTFOperative:
                case ItemType.KeycardMTFCaptain:
                case ItemType.KeycardFacilityManager:
                case ItemType.KeycardChaosInsurgency:
                case ItemType.KeycardO5:
                    return true;
            }

            return false;
        }

        public static Team GetTeam(this RoleTypeId typeId)
        {
            switch (typeId)
            {
                case RoleTypeId.ChaosConscript:
                case RoleTypeId.ChaosRifleman:
                case RoleTypeId.ChaosRepressor:
                case RoleTypeId.ChaosMarauder:
                    return Team.ChaosInsurgency;
                case RoleTypeId.Scientist:
                    return Team.Scientists;
                case RoleTypeId.ClassD:
                    return Team.ClassD;
                case RoleTypeId.Scp173:
                case RoleTypeId.Scp106:
                case RoleTypeId.Scp049:
                case RoleTypeId.Scp079:
                case RoleTypeId.Scp096:
                case RoleTypeId.Scp0492:
                case RoleTypeId.Scp939:
                    return Team.SCPs;
                case RoleTypeId.NtfSpecialist:
                case RoleTypeId.NtfSergeant:
                case RoleTypeId.NtfCaptain:
                case RoleTypeId.NtfPrivate:
                case RoleTypeId.FacilityGuard:
                    return Team.FoundationForces;
                case RoleTypeId.Tutorial:
                    return Team.OtherAlive;
                default:
                    return Team.Dead;
            }
        }

        public static void Broadcast(string text, ushort time, Broadcast.BroadcastFlags broadcastFlags)
        {
            global::Broadcast.Singleton.GetComponent<Broadcast>().RpcAddElement(text, time, broadcastFlags);
        }

        public static void ShowBroadcast(this Player player, string text, ushort time, Broadcast.BroadcastFlags broadcastFlags)
        {
            global::Broadcast.Singleton.GetComponent<Broadcast>().TargetAddElement(player.ReferenceHub.characterClassManager.connectionToClient, text, time, broadcastFlags);
        }

        public static Vector3 GetRandomSpawnLocation(this RoleTypeId roleType)
        {
            if (!PlayerRoleLoader.TryGetRoleTemplate(roleType, out PlayerRoleBase roleBase))
                return Vector3.zero;

            if (roleBase is not IFpcRole fpc)
                return Vector3.zero;

            ISpawnpointHandler spawn = fpc.SpawnpointHandler;
            if (spawn is null)
                return Vector3.zero;

            if (!spawn.TryGetSpawnpoint(out Vector3 pos, out float _))
                return Vector3.zero;

            return pos;
        }

        public static void ChangeAppearance(this Player player, RoleTypeId type)
        {
            foreach (var pl in PlayerList.Where(x => x.PlayerId != player.PlayerId && x.IsReady))
            {
                pl.Connection.Send(new RoleSyncInfo(player.ReferenceHub , type , pl.ReferenceHub));
            }
        }

        public static bool TryGetRoleBase(this RoleTypeId roleType, out PlayerRoleBase roleBase)
        {
            return PlayerRoleLoader.TryGetRoleTemplate(roleType, out roleBase);
        }

        public static IEnumerator<float> PositionCheckerCoroutine(Player player)
        {
            Vector3 position = player.Position;
            float health = player.Health;
            int timeChecker = 0;

            while (true)
            {
                if (player is null || !player.IsAlive || Round.IsRoundEnded || player.Team is not Team.ChaosInsurgency)
                {
                    yield break;
                }

                if (position != player.Position || health.Equals(player.Health))
                {
                    timeChecker = 0;
                    position = player.Position;
                    health = player.Health;
                }
                else
                {
                    timeChecker++;

                    if (timeChecker >= 2)
                    {
                        timeChecker = 0;
                        player.Heal(5f);
                    }
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }

        public static bool IsSameLeadingTeam(this Player player1, Player player2)
        {
            if (player1.Team is Team.FoundationForces && player2.Team is Team.Scientists)
            {
                return true;
            }

            if (player1.Team is Team.ChaosInsurgency && player2.Team is Team.ClassD)
            {
                return true;
            }

            return player1.Team == player2.Team;
        }
    }
}
