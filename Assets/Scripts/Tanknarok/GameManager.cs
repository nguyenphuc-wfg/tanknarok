using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishNetworking.Tanknarok
{
    public class GameManager : MonoBehaviour
    {
        public enum PlayState
        {
            LOBBY,
            LEVEL,
            TRANSITION
        }
        // public static PlayState playState
        // {
        //     get => (instance != null && instance.Object != null && instance.Object.IsValid) ? instance.networkedPlayState : PlayState.LOBBY;
        //     set
        //     {
        //         if (instance != null && instance.Object != null && instance.Object.IsValid)
        //             instance.networkedPlayState = value;
        //     }
        // }

        // public static int WinningPlayerIndex
        // {
        //     get => (instance != null && instance.Object != null && instance.Object.IsValid) ? instance.networkedWinningPlayerIndex : -1;
        //     set
        //     {
        //         if (instance != null && instance.Object != null && instance.Object.IsValid)
        //             instance.networkedWinningPlayerIndex = value;
        //     }
        // }
        public const byte MAX_LIVES = 3;
        public const byte MAX_SCORE = 3;
        private LevelManager _levelManager;
        private bool _restart;

        public static GameManager instance { get; private set; }
    }

}
