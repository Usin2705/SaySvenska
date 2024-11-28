using UnityEngine;
using System.Collections.Generic;

public static class Const
{
    public const int MAX_CHAR_ALLOWED = 50;


    // ===================== AUDIO CONST =====================
    public const int FREQUENCY = 16000;

    // Maximum recording time per seconds
    // The extra record time after the button release is trimmed
    public const int MAX_REC_TIME = 8;

    public const int MAX_REC_TIME_A = 45; //45
    public const int MAX_REC_TIME_B = 30; //30


    public const string REPLAY_FILENAME = "recorded_speech";
    public const string DESCRIBE_FILENAME = "recorded_describe_speech";

    // Used to set the recording time for the audio clip
    // The length of the audio clip depend on the number of characters
    // of the text to be recorded
    public const float SEC_PER_CHAR = 0.12f;

    // Always provide at least 1.5s extra time for recording
    // to avoid the speaker speak too slow and the recording stop
    public const float EXTRA_TIME = 1.5f;

}