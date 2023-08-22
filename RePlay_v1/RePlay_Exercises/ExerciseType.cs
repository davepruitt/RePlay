using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;

namespace RePlay_Exercises
{
    /// <summary>
    /// Enumeration of all supported FitMi exercises
    /// </summary>
    public enum ExerciseType
    {
        //Arm exercises

        [Description("Touches"), EnumMember(Value = "Touches"), DefaultNoiseFloor(5)]
        FitMi_Touches,

        [Description("Reach Across"), EnumMember(Value = "Reach Across"), DefaultNoiseFloor(5)]
        FitMi_ReachAcross,

        [Description("Clapping"), EnumMember(Value = "Clapping"), DefaultNoiseFloor(5)]
        FitMi_Clapping,

        [Description("Reach Out"), EnumMember(Value = "Reach Out"), DefaultNoiseFloor(5)]
        FitMi_ReachOut,

        [Description("Reach Diagonal"), EnumMember(Value = "Reach Diagonal"), DefaultNoiseFloor(5)]
        FitMi_ReachDiagonal,

        [Description("Supination"), EnumMember(Value = "Supination"), DefaultNoiseFloor(10)]
        FitMi_Supination,

        [Description("Bicep Curls"), EnumMember(Value = "Bicep Curls"), DefaultNoiseFloor(10)]
        FitMi_Curls,

        [Description("Shoulder Extension"), EnumMember(Value = "Shoulder Extension"), DefaultNoiseFloor(10)]
        FitMi_ShoulderExtension,

        [Description("Shoulder Abduction"), EnumMember(Value = "Shoulder Abduction"), DefaultNoiseFloor(10)]
        FitMi_ShoulderAbduction,

        [Description("Flyout"), EnumMember(Value = "Flyout"), DefaultNoiseFloor(10)]
        FitMi_Flyout,

        //Hand exercises

        [Description("Wrist Flexion"), EnumMember(Value = "Wrist Flexion"), DefaultNoiseFloor(10)]
        FitMi_WristFlexion,

        [Description("Wrist Deviation"), EnumMember(Value = "Wrist Deviation"), DefaultNoiseFloor(10)]
        FitMi_WristDeviation,

        [Description("Grip"), EnumMember(Value = "Grip"), DefaultNoiseFloor(5)]
        FitMi_Grip,

        [Description("Rotate"), EnumMember(Value = "Rotate"), DefaultNoiseFloor(10)]
        FitMi_Rotate,

        [Description("Key Pinch"), EnumMember(Value = "Key Pinch"), DefaultNoiseFloor(5)]
        FitMi_KeyPinch,

        [Description("Finger Tap"), EnumMember(Value = "Finger Tap"), DefaultNoiseFloor(5)]
        FitMi_FingerTap,

        [Description("Thumb Press"), EnumMember(Value = "Thumb Press"), DefaultNoiseFloor(5)]
        FitMi_ThumbOpposition,

        [Description("Finger Twists"), EnumMember(Value = "Finger Twists"), DefaultNoiseFloor(10)]
        FitMi_FingerTwists,

        [Description("Rolling"), EnumMember(Value = "Rolling"), DefaultNoiseFloor(10)]
        FitMi_Rolling,

        [Description("Flipping"), EnumMember(Value = "Flipping"), DefaultNoiseFloor(10)]
        FitMi_Flipping,

        //CUSTOM FitMi exercises
        
        [Description("Lift"), EnumMember(Value = "Lift"), DefaultNoiseFloor(1.0)]
        FitMiCustom_Lift,
        
        [Description("Generic movement"), EnumMember(Value = "Generic movement"), DefaultNoiseFloor(6)]
        FitMiCustom_Movement,

        [Description("Generic bidirectional movement"), EnumMember(Value = "Generic bidirectional movement"), DefaultNoiseFloor(6)]
        FitMiCustom_MovementBidirectional,

        [Description("Supination and press"), EnumMember(Value = "Supination and press"), DefaultNoiseFloor(1.0)]
        FitMiCustom_FruitArchery,

        //RePlay exercises

        [Description("Isometric Handle"), EnumMember(Value = "Isometric Handle"), DefaultNoiseFloor(1.0)]
        RePlay_Isometric_Handle,

        [Description("Isometric Knob"), EnumMember(Value = "Isometric Knob"), DefaultNoiseFloor(1.0)]
        RePlay_Isometric_Knob,

        [Description("Isometric Wrist"), EnumMember(Value = "Isometric Wrist"), DefaultNoiseFloor(1.0)]
        RePlay_Isometric_Wrist,

        [Description("Isometric Pinch (Right hand)"), EnumMember(Value = "Isometric Pinch"), DefaultNoiseFloor(100)]
        RePlay_Isometric_Pinch,

        [Description("Isometric Pinch (Left hand)"), EnumMember(Value = "Isometric Pinch Left"), DefaultNoiseFloor(100)]
        RePlay_Isometric_PinchLeft,

        [Description("Range of Motion Handle"), EnumMember(Value = "Range of Motion Handle"), DefaultNoiseFloor(10)]
        RePlay_RangeOfMotion_Handle,

        [Description("Range of Motion Knob"), EnumMember(Value = "Range of Motion Knob"), DefaultNoiseFloor(10)]
        RePlay_RangeOfMotion_Knob,

        [Description("Range of Motion Wrist"), EnumMember(Value = "Range of Motion Wrist"), DefaultNoiseFloor(10)]
        RePlay_RangeOfMotion_Wrist,

        //Custom RePlay exercises

        [Description("Isometric Pinch (Right hand, flexion only)"), EnumMember(Value = "Isometric Pinch Flexion"), DefaultNoiseFloor(100)]
        RePlay_Isometric_Pinch_Flexion,

        [Description("Isometric Pinch (Right hand, extension only)"), EnumMember(Value = "Isometric Pinch Extension"), DefaultNoiseFloor(100)]
        RePlay_Isometric_Pinch_Extension,

        [Description("Isometric Pinch (Left hand, flexion only)"), EnumMember(Value = "Isometric Pinch Left Flexion"), DefaultNoiseFloor(100)]
        RePlay_Isometric_Pinch_Left_Flexion,

        [Description("Isometric Pinch (Left hand, extension only)"), EnumMember(Value = "Isometric Pinch Left Extension"), DefaultNoiseFloor(100)]
        RePlay_Isometric_Pinch_Left_Extension,

        //Tablet Touchscreen
        [Description("Touch"), EnumMember(Value = "Touch"), DefaultNoiseFloor(1.0)]
        Touch,

        // Keyboard
        [Description("Typing"), EnumMember(Value = "Typing")]
        Keyboard_Typing,

        [Description("Typing (left handed words)"), EnumMember(Value = "Typing (left handed words)")]
        Keyboard_Typing_LeftHanded,

        [Description("Typing (right handed words)"), EnumMember(Value = "Typing (right handed words)")]
        Keyboard_Typing_RightHanded,

        //Retrieve exercise
        [Description("Find"), EnumMember(Value = "Find")]
        Retrieve_Find,

        //The unknown exercise
        [Description("Unknown"), EnumMember(Value = "Unknown")]
        Unknown
    }
}