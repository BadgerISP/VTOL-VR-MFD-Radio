using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using NAudio.Wave.SampleProviders;
using Steamworks;
using UnityEngine;
using UnityEngine.Audio;
using VTNetworking;
using VTOLVR.Multiplayer;

namespace ListenToStandby.Voice
{
    class DisableStandby : MonoBehaviour
    {
        public void OnEnable()
        {
            ModdedStandbyChannel.Instance.standbyChannel = 0;
        }
    }

    class SetStandbyPatches
    {

        [HarmonyPatch(typeof(ChannelRadioSystem))]
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void PatchStart(ChannelRadioSystem __instance)
        {
            ModdedStandbyChannel.Instance.standbyChannel = (ulong)__instance.standbyChannel;
            if (__instance.gameObject.name == "LSOTeamRadio")
            {
                GameObject disableStandby = new GameObject();
                DisableStandby dis = disableStandby.AddComponent<DisableStandby>();
                disableStandby.transform.parent = __instance.transform;

                dis.OnEnable();
            }
        }

        [HarmonyPatch(typeof(ChannelRadioSystem))]
        [HarmonyPatch("SwapButton")]
        [HarmonyPostfix]
        public static void PatchSwapChannels(ChannelRadioSystem __instance)
        {
            ModdedStandbyChannel.Instance.standbyChannel = (ulong)__instance.standbyChannel;
        }

        [HarmonyPatch(typeof(ChannelRadioSystem))]
        [HarmonyPatch("SetStandbyRadioChannel")]
        [HarmonyPostfix]
        public static void PatchSetStandby(ChannelRadioSystem __instance)
        {
            ModdedStandbyChannel.Instance.standbyChannel = (ulong)__instance.standbyChannel;
        }

        [HarmonyPatch(typeof(ChannelRadioSystem))]
        [HarmonyPatch("RemoteSetFreqs")]
        [HarmonyPostfix]
        public static void PatchRemoteSetFreqs(ChannelRadioSystem __instance)
        {
            ModdedStandbyChannel.Instance.standbyChannel = (ulong)__instance.standbyChannel;
        }
    }

    class PlayStandbyPatches
    {
        [HarmonyPatch(typeof(VTNetworkVoice))]
        [HarmonyPatch("ReceiveVTNetVoiceData")]
        [HarmonyPrefix]
        // look, I apologise sincerely
        public static void PatchReceiveVoice(ulong ___customChannel, ulong incomingID, byte[] buffer, int offset, int count, ulong in_channel, ref byte[] ___voiceDownBuffer, ref MemoryStream ___voiceDownStream, ref MemoryStream ___voiceDecompressedStream, ref float[] ___inFloatBuffer, ref SampleChannel ___sampleProvider)
        {
            if (in_channel == 0L || in_channel == ___customChannel)
            {
                return;
            }

            if (in_channel != ModdedStandbyChannel.Instance.standbyChannel)
            {
                return;
            }

            // this literally just copies the current code for doing this, but plays it on standbySource instead.
            StandbyAudioSources.StandbyAudioSource standbySource;
            if (StandbyAudioSources.Instance.sources.TryGetValue(incomingID, out standbySource) && (VTNetworkVoice.mutes == null || !VTNetworkVoice.mutes.Contains(incomingID)))
            {
                Buffer.BlockCopy(buffer, offset, ___voiceDownBuffer, 0, count);
                lock (standbySource.inStreamLock)
                {
                    ___voiceDownStream.Position = 0L;
                    ___voiceDecompressedStream.Position = 0L;
                    int num = SteamUser.DecompressVoice(___voiceDownStream, count, ___voiceDecompressedStream) / 2;
                    ___voiceDecompressedStream.Position = 0L;
                    if (___inFloatBuffer.Length < num)
                    {
                        ___inFloatBuffer = new float[num];
                        Debug.Log(string.Format("VTNetworkVoice: new float buffer length: {0}", num));
                    }
                    ___sampleProvider.Read(___inFloatBuffer, 0, num);
                    for (int i = 0; i < num; i++)
                    {
                        standbySource.sampleQueue.Enqueue(___inFloatBuffer[i]);
                    }
                }
            }

            return;
        }
    }

    class AddStandbyMFDPagePatch
    {
        [HarmonyPatch(typeof(MFDManager))]
        [HarmonyPatch("SetupDict")]
        [HarmonyPostfix]
        public static void AddStandbyPage(MFDManager __instance)
        {
            var pagesDic = AccessTools.FieldRefAccess<Dictionary<string, MFDPage>>(typeof(MFDManager), "pagesDic")(__instance);
            var mfdPages = AccessTools.FieldRefAccess<List<MFDPage>>(typeof(MFDManager), "mfdPages")(__instance);

            if (__instance == null || pagesDic.ContainsKey("STBY"))
            {
                return;
            }

            GameObject standbyPageObject = new GameObject("ListenToStandby_STBYPage");
            standbyPageObject.transform.SetParent(__instance.transform, false);

            StandbyMFDPage standbyPage = standbyPageObject.AddComponent<StandbyMFDPage>();
            standbyPage.manager = __instance;
            standbyPage.pageName = "STBY";
            standbyPage.SetText("STBY", "MFD Radio Page");
            standbyPage.SetPageButtons(new[]
            {
                new MFDPage.MFDButtonInfo { button = MFD.MFDButtons.L1, label = "Enable" },
                new MFDPage.MFDButtonInfo { button = MFD.MFDButtons.L2, label = "Cycle" },
                new MFDPage.MFDButtonInfo { button = MFD.MFDButtons.R1, label = "Slot 1" },
                new MFDPage.MFDButtonInfo { button = MFD.MFDButtons.R2, label = "Slot 2" },
                new MFDPage.MFDButtonInfo { button = MFD.MFDButtons.R3, label = "Slot 3" },
                new MFDPage.MFDButtonInfo { button = MFD.MFDButtons.R4, label = "Slot 4" },
            });

            if (!pagesDic.ContainsKey("STBY"))
            {
                pagesDic.Add("STBY", standbyPage);
            }

            if (!mfdPages.Contains(standbyPage))
            {
                mfdPages.Add(standbyPage);
            }
        }
    }

    class AddStandbyPatches
    {
        [HarmonyPatch(typeof(CockpitTeamRadioManager))]
        [HarmonyPatch("SetupVoiceSource")]
        [HarmonyPostfix]
        public static void AddStandbyVoice(PlayerInfo player, Transform ___opforSourcePosition)
        {
            StandbyAudioSources.Instance.CreateForPlayer(player, ___opforSourcePosition);
        }

        [HarmonyPatch(typeof(CockpitTeamRadioManager))]
        [HarmonyPatch("RemovePlayer")]
        [HarmonyPostfix]
        public static void RemovePlayer(PlayerInfo player)
        {
            if (player == null)
            {
                return;
            }
            StandbyAudioSources.Instance.DestoryPlayer(player);
        }
    }

    class DontChangeOpforVolumePatch
    {
        [HarmonyPatch(typeof(CommRadioManager))]
        [HarmonyPatch("SetCommsVolumeMP")]
        [HarmonyPrefix]
        public static bool DisableChangeOpfor(float t, AudioMixerGroup ___mpAlliedMixerGroup)
        {
            float num = Mathf.Lerp(-30f, 8f, Mathf.Sqrt(t));
            ___mpAlliedMixerGroup.audioMixer.SetFloat("CommAttenuationAllied", num);

            return false;
        }
    }
}
