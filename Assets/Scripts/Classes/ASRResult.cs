using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Represents the result of an Automatic Speech Recognition (ASR) process.
/// </summary>
[System.Serializable]
public class ASRResult
{
    /// <summary>
    /// Gets or sets the list of Levenshtein operations.
    /// </summary>
    public List<OPS> levenshtein;

    /// <summary>
    /// Gets or sets the predicted text from the ASR process.
    /// </summary>
    public string prediction;

    /// <summary>
    /// Gets or sets the list of scores associated with the ASR result.
    /// </summary>
    public List<float> score;
}


[System.Serializable]
public class OPS 
{   
    /*
    *
    *   There's 3 code in ops: replace, insert, delete
    *   there's "equal" which is not used in the python code so it won't show up here
    *
    */

    public string ops;
    public int tran_index;
    public int pred_index;
}