using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Xna.Framework.Input;
using RePlay_Activity_Common;
using RePlay_Exercises;

namespace RePlay_Activity_Breakout
{
    class KeyboardExercise_LeftRight : ExerciseBase
    {
        #region Private data members

        private List<Keys> previous_frame_pressed_keys = new List<Keys>();
        private RePlay_Game_Activity game_activity;
        private BreakoutGame breakout_game;

        #endregion

        #region Constructor

        public KeyboardExercise_LeftRight (BreakoutGame game, RePlay_Game_Activity a, double gain = 1)
            : base(gain)
        {
            CurrentActivity = a;
            game_activity = a;
            breakout_game = game;
        }

        #endregion

        #region Overrides
        
        public override void Update()
        {
            KeyboardState key_state = Keyboard.GetState();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            var pressed_keys = key_state.GetPressedKeys().ToList();
            var released_keys = previous_frame_pressed_keys.Where(x => !pressed_keys.Contains(x)).ToList();
            previous_frame_pressed_keys = pressed_keys;

            if (pressed_keys.Any(x => x == Keys.Left))
            {
                CurrentActualValue = -50;
            }
            else if (pressed_keys.Any(x => x == Keys.Right))
            {
                CurrentActualValue = 50;
            }
            else
            {
                CurrentActualValue = 0;
            }
        }
        #endregion
    }
}