using UnityEngine;
using System.Collections.Generic;

public static class Const
{
    // ================= TEXT COLOR SCORE ===================
    // Rich text color tag for each type of scoring
    public const string BAD_COLOR = "#D43C3C";    
    public const string AVG_COLOR = "#F38B1E";
    public const string GOOD_COLOR = "#4CA415";    

    // Maximum for bad string is 9 length (for UI to look nice in most case), ideal was "Incorrect"
    public const string BAD_STRING = "Flawed";    
    public const string AVG_STRING = "Almost correct";
    // The onboarding is manual text input so we don't use this for now
    public const string GOOD_STRING = "Correct";    

    /* Score range (less than) for each type of scoring
    *  The bad score actually depend on the model
    *  Without finetuned, 3rd quartile (75%) of error from FA score
    *  is about 45%, which then 0.49f sound good (mathematically also good)
    *  Then it's hard to select avg_score, but let's just pick 0.92f
    *  
    *  For finetuned models with digitala, 3rd quartile only about 29%, so  
    *  we can select something around 0.3f or 0.35f
    *  This will also allow more room for addtional score range 
    *  (from bad to more than 50% would be another option)
    *
    *  The average score then should also be lower, different model will have 
    *  different scale, but a rule of thumb would be anything below 0.9f is not 
    *  NATIVE level, so avg score should be around 0.9f
    *  Some finetuned model could result in lower AVG_SCORE
    *  And some model allow for addtional scale (BAD, AVG, GOOD, EXCELLENT)
    *  
    */
    public const float BAD_SCORE = 0.30f;
    public const float AVG_SCORE = 0.80f;
    //public const float GOOD_SCORE = 0.92f;

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

    // Always provide at least 1.8s extra time for recording
    // to avoid the speaker speak too slow and the recording stop
    public const float EXTRA_TIME = 1.8f;

    // ===================== NETWORK CONST =====================

    // Maximum waiting time for Unity web request
    public const int TIME_OUT_SECS = 20;
    public const int TIME_OUT_ADVANCE_SECS = 30;

    public const string FILE_NAME_POST = "speech_sample";

    // ===================== PLAYER PREFS CONST =====================
    public const string PREF_TEMPERATURE = "pref_temperature";
    
    public const string PREF_TOPK = "pref_topk";
}